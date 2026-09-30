import json
import time
from typing import Optional, Any
from uuid import UUID

from smartgym_ai.agents.base_agent import AgentBase
from smartgym_ai.models.workflow_models import WorkflowState, ToolExecutionRecord
from smartgym_ai.models.action_models import (
    ApprovalStatus,
    ActionAgentInput,
    ActionAgentOutput,
)
from smartgym_ai.tools.base_tool import ToolBase, ToolResult
from smartgym_ai.tools.action_tools import (
    CreateRepairOrderTool,
    PrepareVendorEmailTool,
    SendVendorEmailTool,
    UpdateFacilityIssueStatusTool,
    CreateNotificationTool,
)
from smartgym_ai.services.tool_persistence import tool_persistence_service
from smartgym_ai.services.resilience import RetryManager, TimeoutHandler
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.services.logging import ai_logger, get_correlation_id

ACTION_AGENT_SYSTEM_PROMPT = """You are the SmartGym Action / Tool Agent.
Your responsibility is to prepare and execute approved SmartGym business actions,
including creating official repair orders, drafting vendor RFQ communications,
dispatching vendor orders upon confirmed authorization, updating facility issue status,
and sending operational notifications.

CRITICAL SECURITY RULE (MANDATORY):
You must NOT execute high-impact actions (such as sending vendor emails or dispatching orders)
until authorization is confirmed by an ADMIN or FACILITY MANAGER.
Any attempt to execute high-impact actions without an APPROVED status must be strictly rejected
by application logic.
"""


class ActionExecutionAgent(AgentBase):
    """
    Action / Tool Agent for SmartGym.
    Prepares and executes approved business operations with deterministic authorization gating,
    idempotency enforcement, audit logging, and observable tool execution.
    """

    def __init__(
        self,
        llm_client: Optional[ILLMClient] = None,
        db_url: Optional[str] = None,
        tool_timeout_seconds: float = 10.0,
        max_tool_retries: int = 2
    ):
        if llm_client is None:
            from smartgym_ai.services.llm.mock_client import MockLLMClient
            llm_client = MockLLMClient()

        self.db_url = db_url
        self.tool_timeout_seconds = tool_timeout_seconds
        self.max_tool_retries = max_tool_retries

        self.create_order_tool = CreateRepairOrderTool(db_url=db_url)
        self.prepare_email_tool = PrepareVendorEmailTool()
        self.send_email_tool = SendVendorEmailTool()
        self.update_status_tool = UpdateFacilityIssueStatusTool(db_url=db_url)
        self.create_notification_tool = CreateNotificationTool(db_url=db_url)

        tools = [
            self.create_order_tool,
            self.prepare_email_tool,
            self.send_email_tool,
            self.update_status_tool,
            self.create_notification_tool,
        ]

        super().__init__(
            name="ActionExecutionAgent",
            role="Action / Tool Agent",
            system_prompt=ACTION_AGENT_SYSTEM_PROMPT,
            llm_client=llm_client,
            tools=tools
        )

    async def execute_tool_with_observability(
        self,
        tool: ToolBase,
        params: dict[str, Any],
        step_id: Optional[UUID] = None
    ) -> ToolResult:
        """Executes tool with timeout, retry, logging, and database persistence."""
        start_time = time.perf_counter()
        tool_name = tool.name
        retry_manager = RetryManager(max_retries=self.max_tool_retries, backoff_factor=0.1)

        ai_logger.info(
            f"ActionExecutionAgent invoking tool '{tool_name}'",
            extra={
                "correlation_id": get_correlation_id(),
                "extra_data": {"tool": tool_name, "params": {k: v for k, v in params.items() if k != "body"}}
            }
        )

        async def _run_tool():
            return await TimeoutHandler.execute_with_timeout(
                tool.execute(**params),
                timeout_seconds=self.tool_timeout_seconds,
                operation_name=f"Tool.{tool_name}"
            )

        try:
            result: ToolResult = await retry_manager.execute_with_retry(_run_tool)
        except Exception as e:
            elapsed_ms = int((time.perf_counter() - start_time) * 1000)
            result = ToolResult(
                tool_name=tool_name,
                is_success=False,
                error=str(e),
                execution_time_ms=elapsed_ms
            )

        try:
            tool_persistence_service.record_execution(
                tool_name=tool_name,
                input_params=params,
                output_result=result.data if result.is_success else {"error": result.error},
                is_success=result.is_success,
                execution_time_ms=result.execution_time_ms,
                workflow_step_id=step_id
            )
        except Exception as pe:
            ai_logger.warning(f"Could not persist tool execution for '{tool_name}': {pe}")

        return result

    async def execute_action_plan(
        self,
        input_data: ActionAgentInput,
        step_id: Optional[UUID] = None
    ) -> tuple[ActionAgentOutput, list[ToolExecutionRecord]]:
        """
        Executes action pipeline enforcing human approval security.
        """
        tool_records: list[ToolExecutionRecord] = []
        execution_plan: list[str] = []

        diag = input_data.domain_recommendation or {}
        wf_ctx = input_data.workflow_context or {}
        app_ctx = input_data.approval_context or {}

        # Extract approval details
        raw_status = app_ctx.get("approval_status") or app_ctx.get("status") or "PENDING"
        approval_status = ApprovalStatus(str(raw_status).upper()) if str(raw_status).upper() in ApprovalStatus.__members__ else ApprovalStatus.PENDING
        is_approved = (approval_status == ApprovalStatus.APPROVED)

        issue_id = wf_ctx.get("issue_id") or diag.get("issue_id")
        equipment_id = wf_ctx.get("equipment_id") or diag.get("equipment_id")
        equipment_name = wf_ctx.get("equipment_name") or diag.get("equipment_name") or "Gym Equipment"
        estimated_cost = float(diag.get("estimated_cost") or diag.get("estimatedCost") or 0.0)
        supplier_name = diag.get("recommended_supplier") or diag.get("recommendedSupplier") or "LifeFitness USA"
        supplier_id = diag.get("supplier_id") or diag.get("supplierId")
        required_part = diag.get("required_part") or diag.get("requiredPart")
        possible_issue = diag.get("possible_issue") or diag.get("possibleIssue") or "Facility maintenance requirement"

        # Check proposal integrity / revalidation rule:
        # "Changed proposal after approval requires revalidation."
        original_approved_cost = app_ctx.get("approved_estimated_cost")
        if original_approved_cost is not None and abs(float(original_approved_cost) - estimated_cost) > 0.01:
            ai_logger.warning(
                f"Proposal cost changed from {original_approved_cost} to {estimated_cost} after approval. Requiring revalidation.",
                extra={"correlation_id": get_correlation_id()}
            )
            is_approved = False
            approval_status = ApprovalStatus.REVISION_REQUIRED
            execution_plan.append("Proposal changed after prior approval; revalidation required by management.")

        # 1. Create or Update Repair Order
        ro_res = await self.execute_tool_with_observability(
            self.create_order_tool,
            {
                "issue_id": str(issue_id),
                "equipment_id": str(equipment_id) if equipment_id else None,
                "estimated_cost": estimated_cost,
                "technician_name": "Certified Technician",
                "supplier_id": str(supplier_id) if supplier_id else None,
                "is_approved": is_approved
            },
            step_id=step_id
        )
        tool_records.append(ToolExecutionRecord(
            tool_name=self.create_order_tool.name,
            input_parameters={"issue_id": str(issue_id), "is_approved": is_approved},
            output_result=ro_res.data,
            is_success=ro_res.is_success,
            execution_time_ms=ro_res.execution_time_ms
        ))

        repair_order_id = None
        order_num = None
        if ro_res.is_success and isinstance(ro_res.data, dict):
            repair_order_id = ro_res.data.get("repair_order_id")
            order_num = ro_res.data.get("order_number")
            execution_plan.append(f"Repair Order #{order_num} ({ro_res.data.get('action')}, Status: {ro_res.data.get('status')})")

        # 2. Prepare Vendor Email Draft
        draft_res = await self.execute_tool_with_observability(
            self.prepare_email_tool,
            {
                "supplier_name": supplier_name,
                "equipment_name": equipment_name,
                "required_part": required_part,
                "issue_description": possible_issue,
                "estimated_cost": estimated_cost
            },
            step_id=step_id
        )
        tool_records.append(ToolExecutionRecord(
            tool_name=self.prepare_email_tool.name,
            input_parameters={"supplier_name": supplier_name, "equipment_name": equipment_name},
            output_result=draft_res.data,
            is_success=draft_res.is_success,
            execution_time_ms=draft_res.execution_time_ms
        ))

        email_draft = draft_res.data if (draft_res.is_success and isinstance(draft_res.data, dict)) else {}
        execution_plan.append(f"Prepared RFQ vendor email draft for '{supplier_name}'")

        # 3. HIGH-IMPACT ACTION: Send Vendor Email (STRICTLY GATED)
        idempotency_key = f"vendor-action-{issue_id}-{order_num or 'draft'}"
        if is_approved:
            send_res = await self.execute_tool_with_observability(
                self.send_email_tool,
                {
                    "recipient": email_draft.get("recipient", f"support@{supplier_name.lower().replace(' ', '')}.com"),
                    "subject": email_draft.get("subject", "SmartGym Maintenance Dispatch"),
                    "body": email_draft.get("body", "Authorized maintenance dispatch."),
                    "approval_status": approval_status.value,
                    "idempotency_key": idempotency_key,
                    "workflow_id": str(wf_ctx.get("workflow_id", ""))
                },
                step_id=step_id
            )
            tool_records.append(ToolExecutionRecord(
                tool_name=self.send_email_tool.name,
                input_parameters={"recipient": email_draft.get("recipient"), "approval_status": approval_status.value},
                output_result=send_res.data,
                is_success=send_res.is_success,
                execution_time_ms=send_res.execution_time_ms
            ))
            if send_res.is_success and send_res.data.get("sent"):
                execution_plan.append(f"Dispatched vendor email to '{email_draft.get('recipient')}' (Message ID: {send_res.data.get('message_id')})")
            elif send_res.is_success and send_res.data.get("is_duplicate"):
                execution_plan.append("Duplicate vendor email dispatch prevented (Idempotent replay detected).")
            else:
                execution_plan.append(f"Vendor email transmission halted: {send_res.error}")

            # 4. Update Facility Issue Status -> VENDOR_CONTACTED
            status_res = await self.execute_tool_with_observability(
                self.update_status_tool,
                {
                    "issue_id": str(issue_id),
                    "status": "VENDOR_CONTACTED",
                    "resolution_notes": f"Approved by management. Repair order #{order_num} issued and vendor contacted."
                },
                step_id=step_id
            )
            tool_records.append(ToolExecutionRecord(
                tool_name=self.update_status_tool.name,
                input_parameters={"issue_id": str(issue_id), "status": "VENDOR_CONTACTED"},
                output_result=status_res.data,
                is_success=status_res.is_success,
                execution_time_ms=status_res.execution_time_ms
            ))
            execution_plan.append("Updated facility issue status to 'VENDOR_CONTACTED'")

            # 5. Create Staff Notification
            notif_res = await self.execute_tool_with_observability(
                self.create_notification_tool,
                {
                    "title": f"Facility Issue Action Executed: {equipment_name}",
                    "message": f"Repair order #{order_num} has been authorized and dispatched to {supplier_name} (Cost: LKR {estimated_cost:,.2f}).",
                    "notification_type": 3
                },
                step_id=step_id
            )
            tool_records.append(ToolExecutionRecord(
                tool_name=self.create_notification_tool.name,
                input_parameters={"title": f"Facility Issue Action Executed: {equipment_name}"},
                output_result=notif_res.data,
                is_success=notif_res.is_success,
                execution_time_ms=notif_res.execution_time_ms
            ))
            execution_plan.append("Created staff notification for scheduled maintenance")

            proposed_action = f"Executed authorized repair order #{order_num} with {supplier_name} (LKR {estimated_cost:,.2f})."
        else:
            # Action paused pending approval
            execution_plan.append(
                f"High-impact action paused: Approval status is '{approval_status.value}'. "
                "Vendor communication cannot be executed until approved by Admin/Facility Manager."
            )
            proposed_action = (
                f"Prepared repair order #{order_num or 'PENDING'} and RFQ draft for {supplier_name}. "
                "Awaiting mandatory human authorization."
            )

        output = ActionAgentOutput(
            proposedAction=proposed_action,
            repairOrderId=repair_order_id,
            supplierId=str(supplier_id) if supplier_id else None,
            estimatedCost=estimated_cost,
            executionPlan=execution_plan
        )

        return output, tool_records

    async def _process(self, state: WorkflowState, context: dict[str, Any]) -> dict[str, Any]:
        """LangGraph execution lifecycle implementation."""
        domain_recommendation = state.structured_output.get("domain_analysis") or state.structured_output.get("diagnosis") or {}
        workflow_context = {
            "workflow_id": str(state.id),
            "issue_id": str(state.issue_id),
            "issue_title": state.issue_title,
            "equipment_name": state.equipment_name,
            "equipment_id": context.get("equipment_id")
        }

        # Check approval state on workflow
        raw_approval = state.human_approval_granted
        if raw_approval is True:
            approval_status = ApprovalStatus.APPROVED.value
        elif raw_approval is False:
            approval_status = ApprovalStatus.REJECTED.value
        else:
            approval_status = ApprovalStatus.PENDING.value

        approval_context = {
            "approval_status": approval_status,
            "approver_id": context.get("approver_id"),
            "approver_role": context.get("approver_role"),
            "comments": context.get("approval_comments")
        }

        input_data = ActionAgentInput(
            domain_recommendation=domain_recommendation,
            workflow_context=workflow_context,
            approval_context=approval_context
        )

        output, tool_records = await self.execute_action_plan(input_data, step_id=state.id)
        state.tool_executions.extend(tool_records)

        return {
            "proposedAction": output.proposed_action,
            "repairOrderId": output.repair_order_id,
            "supplierId": output.supplier_id,
            "estimatedCost": output.estimated_cost,
            "executionPlan": output.execution_plan,
            "summary": output.proposed_action
        }

    async def run(self, input_data: ActionAgentInput, step_id: Optional[UUID] = None) -> ActionAgentOutput:
        """
        Executes action pipeline directly from ActionAgentInput and returns ActionAgentOutput.
        """
        output, _ = await self.execute_action_plan(input_data, step_id=step_id)
        return output
