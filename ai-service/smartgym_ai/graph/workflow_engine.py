from typing import TypedDict, Optional, Any, Literal
from uuid import UUID, uuid4
from datetime import datetime, timezone
from pydantic import BaseModel, Field

from langgraph.graph import StateGraph, END

from smartgym_ai.configuration.settings import settings
from smartgym_ai.models.workflow_models import (
    WorkflowState,
    WorkflowStatus,
    WorkflowStepState,
    WorkflowStepStatus,
    WorkflowStartRequest,
    WorkflowResumeRequest,
    ValidationResultRecord,
)
from smartgym_ai.models.planner_models import (
    PlannerInput,
    PlannerOutput,
    PlannedStep,
)
from smartgym_ai.agents.planner_agent import PlannerAgent
from smartgym_ai.services.state_store import IWorkflowStateStore, workflow_store
from smartgym_ai.services.llm.factory import get_llm_client
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.services.resilience import SafeFailureHandler
from smartgym_ai.services.logging import ai_logger, get_correlation_id, set_correlation_id, set_workflow_id


class DiagnosisOutputSchema(BaseModel):
    """Structured diagnosis output schema for LLM calls."""
    diagnosis_summary: str = Field(description="Summary of the root cause diagnosis")
    recommended_action: str = Field(description="Remediation or repair action recommended")
    estimated_cost: float = Field(description="Estimated repair or replacement cost in LKR")
    confidence_score: float = Field(default=0.9, description="Confidence score from 0.0 to 1.0")


class WorkflowGraphState(TypedDict):
    """State definition for LangGraph state machine."""
    workflow_id: str
    correlation_id: str
    issue_id: str
    issue_title: str
    equipment_name: str
    description: str
    status: str
    current_step: str
    estimated_cost: float
    requires_human_approval: bool
    human_approval_granted: Optional[bool]
    diagnosis_summary: str
    recommended_action: str
    error_details: Optional[str]
    structured_output: dict[str, Any]
    # Planner agent contracts
    objective: str
    plan_id: Optional[str]
    planned_steps: list[dict[str, Any]]
    assigned_agent: Optional[str]
    next_agent: Optional[str]
    next_step: Optional[str]
    planning_reason: Optional[str]


class WorkflowEngine:
    """
    Stateful workflow orchestration engine built on LangGraph.
    Manages end-to-end execution of AI workflows, Planner Agent integration,
    HITL approval gates, resilience fallbacks, and state persistence.
    """

    def __init__(
        self,
        store: Optional[IWorkflowStateStore] = None,
        llm_client: Optional[ILLMClient] = None
    ):
        self.store = store or workflow_store
        self.llm_client = llm_client or get_llm_client()
        self.planner_agent = PlannerAgent(self.llm_client)
        self.graph = self._build_graph()

    def _build_graph(self):
        workflow = StateGraph(WorkflowGraphState)

        # Nodes
        workflow.add_node("initialize", self._initialize_node)
        workflow.add_node("planner", self._planner_node)
        workflow.add_node("evaluate_diagnosis", self._evaluate_diagnosis_node)
        workflow.add_node("approval_gate", self._approval_gate_node)
        workflow.add_node("execution", self._execution_node)
        workflow.add_node("safe_failure", self._safe_failure_node)

        # Entry edge
        workflow.set_entry_point("initialize")
        workflow.add_edge("initialize", "planner")

        # Routing after Planner
        workflow.add_conditional_edges(
            "planner",
            self._route_after_planner,
            {
                "evaluate_diagnosis": "evaluate_diagnosis",
                "safe_failure": "safe_failure"
            }
        )

        # Routing after Diagnosis
        workflow.add_conditional_edges(
            "evaluate_diagnosis",
            self._route_after_diagnosis,
            {
                "approval_gate": "approval_gate",
                "safe_failure": "safe_failure"
            }
        )

        # Routing after Approval Gate
        workflow.add_conditional_edges(
            "approval_gate",
            self._route_after_gate,
            {
                "execution": "execution",
                "paused": END,
                "rejected": END,
                "safe_failure": "safe_failure"
            }
        )

        workflow.add_edge("execution", END)
        workflow.add_edge("safe_failure", END)

        return workflow.compile()

    # --- Node Implementations ---

    async def _initialize_node(self, state: WorkflowGraphState) -> dict[str, Any]:
        return {
            "status": WorkflowStatus.Running.value,
            "current_step": "Initialization"
        }

    async def _planner_node(self, state: WorkflowGraphState) -> dict[str, Any]:
        """
        Planner Agent Node:
        Receives domain objective, creates structured multi-step plan,
        and delegates steps to specialized agents.
        """
        try:
            planner_input = PlannerInput(
                workflow_id=UUID(state["workflow_id"]),
                facility_issue_id=UUID(state["issue_id"]),
                objective=state.get("objective") or f"Resolve facility issue: {state['issue_title']} on {state['equipment_name']}",
                request_summary=state.get("description") or state["issue_title"],
                equipment_name=state.get("equipment_name", "Gym Equipment"),
                metadata={"issue_title": state["issue_title"]}
            )

            plan: PlannerOutput = await self.planner_agent.create_plan(planner_input)

            first_step = plan.steps[0] if plan.steps else None
            next_step_name = first_step.step_name if first_step else "evaluate_diagnosis"
            next_agent_name = first_step.assigned_agent if first_step else "DomainAnalysisAgent"

            structured = dict(state.get("structured_output") or {})
            structured["plan"] = plan.model_dump()
            structured["plan_id"] = str(plan.plan_id)
            structured["planned_steps"] = [s.model_dump() for s in plan.steps]
            structured["next_agent"] = next_agent_name
            structured["next_step"] = next_step_name

            return {
                "status": WorkflowStatus.Planning.value,
                "current_step": "Planning Complete",
                "plan_id": str(plan.plan_id),
                "planned_steps": [s.model_dump() for s in plan.steps],
                "assigned_agent": plan.assigned_agent,
                "next_agent": next_agent_name,
                "next_step": next_step_name,
                "planning_reason": plan.reason,
                "requires_human_approval": plan.approval_required,
                "structured_output": structured,
                "error_details": None
            }
        except Exception as e:
            return {
                "error_details": f"Planner failed: {str(e)}"
            }

    async def _evaluate_diagnosis_node(self, state: WorkflowGraphState) -> dict[str, Any]:
        try:
            prompt = [
                {
                    "role": "system",
                    "content": (
                        "You are a Senior Gym Facility Technical Analyst. "
                        "Analyze reported equipment issues, diagnose the root cause, "
                        "estimate the repair cost in Sri Lankan Rupees (LKR), and recommend action."
                    )
                },
                {
                    "role": "user",
                    "content": f"Equipment: {state['equipment_name']}\nIssue: {state['issue_title']}\nDescription: {state['description']}"
                }
            ]

            result: DiagnosisOutputSchema = await self.llm_client.generate_structured(
                prompt,
                schema=DiagnosisOutputSchema
            )

            # Determine if cost exceeds Human-In-The-Loop threshold
            threshold = settings.AI_APPROVAL_COST_THRESHOLD
            requires_approval = result.estimated_cost >= threshold or state.get("requires_human_approval", False)

            structured = dict(state.get("structured_output") or {})
            structured["diagnosis"] = result.model_dump()

            return {
                "diagnosis_summary": result.diagnosis_summary,
                "recommended_action": result.recommended_action,
                "estimated_cost": result.estimated_cost,
                "requires_human_approval": requires_approval,
                "structured_output": structured,
                "error_details": None
            }
        except Exception as e:
            return {
                "error_details": f"Diagnosis failed: {str(e)}"
            }

    async def _approval_gate_node(self, state: WorkflowGraphState) -> dict[str, Any]:
        # Check approval state
        if state.get("requires_human_approval") and state.get("human_approval_granted") is None:
            return {
                "status": WorkflowStatus.AwaitingApproval.value,
                "current_step": "Awaiting Human Approval (Cost Threshold Exceeded)"
            }
        elif state.get("human_approval_granted") is False:
            return {
                "status": WorkflowStatus.Rejected.value,
                "current_step": "Workflow Rejected by Human Manager"
            }
        else:
            return {
                "status": WorkflowStatus.Executing.value,
                "current_step": "Action Execution"
            }

    async def _execution_node(self, state: WorkflowGraphState) -> dict[str, Any]:
        return {
            "status": WorkflowStatus.Completed.value,
            "current_step": "Remediation Complete"
        }

    async def _safe_failure_node(self, state: WorkflowGraphState) -> dict[str, Any]:
        return {
            "status": WorkflowStatus.Failed.value,
            "current_step": "Safe Failure Handled",
            "recommended_action": "Escalated for immediate technician on-site review."
        }

    # --- Routing Conditions ---

    def _route_after_planner(self, state: WorkflowGraphState) -> Literal["evaluate_diagnosis", "safe_failure"]:
        if state.get("error_details"):
            return "safe_failure"
        return "evaluate_diagnosis"

    def _route_after_diagnosis(self, state: WorkflowGraphState) -> Literal["approval_gate", "safe_failure"]:
        if state.get("error_details"):
            return "safe_failure"
        return "approval_gate"

    def _route_after_gate(self, state: WorkflowGraphState) -> Literal["execution", "paused", "rejected", "safe_failure"]:
        if state.get("error_details"):
            return "safe_failure"
        if state.get("status") == WorkflowStatus.AwaitingApproval.value:
            return "paused"
        if state.get("status") == WorkflowStatus.Rejected.value:
            return "rejected"
        return "execution"

    # --- Public API Orchestration Methods ---

    async def start_workflow(self, request: WorkflowStartRequest) -> WorkflowState:
        cid = request.correlation_id or get_correlation_id()
        set_correlation_id(cid)

        state = WorkflowState(
            issue_id=request.issue_id,
            issue_title=request.issue_title,
            equipment_name=request.equipment_name or "Gym Equipment",
            workflow_type=request.workflow_type,
            status=WorkflowStatus.Initiated,
            current_step="Initialization",
            correlation_id=cid,
            model_identifier=f"{self.llm_client.provider_name}/{self.llm_client.model_name}"
        )
        set_workflow_id(str(state.id))

        # Add initial step
        state.steps.append(
            WorkflowStepState(
                step_name="Initialization",
                step_order=1,
                status=WorkflowStepStatus.Completed,
                summary=f"Workflow initiated for issue '{request.issue_title}'"
            )
        )

        self.store.create(state)

        # Prepare initial LangGraph state
        graph_input: WorkflowGraphState = {
            "workflow_id": str(state.id),
            "correlation_id": cid,
            "issue_id": str(request.issue_id),
            "issue_title": request.issue_title,
            "equipment_name": request.equipment_name or "Gym Equipment",
            "description": request.description,
            "status": state.status.value,
            "current_step": state.current_step,
            "estimated_cost": 0.0,
            "requires_human_approval": False,
            "human_approval_granted": None,
            "diagnosis_summary": "",
            "recommended_action": "",
            "error_details": None,
            "structured_output": {},
            "objective": f"Resolve facility issue: {request.issue_title} on {request.equipment_name or 'Gym Equipment'}",
            "plan_id": None,
            "planned_steps": [],
            "assigned_agent": None,
            "next_agent": None,
            "next_step": None,
            "planning_reason": None,
        }

        # Invoke graph
        output = await self.graph.ainvoke(graph_input)

        # Sync back to domain WorkflowState
        self._sync_graph_output_to_state(state, output)
        return self.store.update(state)

    async def resume_workflow(self, workflow_id: UUID, resume_request: WorkflowResumeRequest) -> WorkflowState:
        state = self.store.get(workflow_id)
        if not state:
            raise KeyError(f"Workflow {workflow_id} not found.")

        set_correlation_id(state.correlation_id)
        set_workflow_id(str(state.id))

        action = resume_request.action.lower()
        if action == "approve":
            state.human_approval_granted = True
            state.status = WorkflowStatus.Executing
            state.validation_results.append(
                ValidationResultRecord(
                    rule_name="HumanApprovalGate",
                    passed=True,
                    validation_message=f"Approved: {resume_request.comments or 'Cost approved by manager.'}"
                )
            )
        elif action == "reject":
            state.human_approval_granted = False
            state.status = WorkflowStatus.Rejected
            state.completed_at = datetime.now(timezone.utc)
            state.validation_results.append(
                ValidationResultRecord(
                    rule_name="HumanApprovalGate",
                    passed=False,
                    validation_message=f"Rejected: {resume_request.comments or 'Rejected by manager.'}"
                )
            )
            return self.store.update(state)

        # Resume through LangGraph
        graph_input: WorkflowGraphState = {
            "workflow_id": str(state.id),
            "correlation_id": state.correlation_id,
            "issue_id": str(state.issue_id),
            "issue_title": state.issue_title,
            "equipment_name": state.equipment_name,
            "description": state.diagnosis_summary,
            "status": state.status.value,
            "current_step": "Action Execution",
            "estimated_cost": state.estimated_cost or 0.0,
            "requires_human_approval": state.requires_human_approval,
            "human_approval_granted": state.human_approval_granted,
            "diagnosis_summary": state.diagnosis_summary,
            "recommended_action": state.recommended_action,
            "error_details": None,
            "structured_output": state.structured_output or {},
            "objective": f"Resume resolution for {state.equipment_name}",
            "plan_id": state.structured_output.get("plan_id") if state.structured_output else None,
            "planned_steps": state.structured_output.get("planned_steps", []) if state.structured_output else [],
            "assigned_agent": state.structured_output.get("assigned_agent") if state.structured_output else None,
            "next_agent": "ActionExecutionAgent",
            "next_step": "Action Execution",
            "planning_reason": None,
        }

        output = await self.graph.ainvoke(graph_input)
        self._sync_graph_output_to_state(state, output)
        return self.store.update(state)

    async def cancel_workflow(self, workflow_id: UUID, reason: Optional[str] = None) -> WorkflowState:
        state = self.store.get(workflow_id)
        if not state:
            raise KeyError(f"Workflow {workflow_id} not found.")

        state.status = WorkflowStatus.Cancelled
        state.current_step = "Cancelled"
        state.error_details = reason or "Cancelled by user/manager."
        state.completed_at = datetime.now(timezone.utc)

        state.steps.append(
            WorkflowStepState(
                step_name="Cancellation",
                step_order=len(state.steps) + 1,
                status=WorkflowStepStatus.Completed,
                summary=f"Workflow cancelled: {state.error_details}"
            )
        )

        return self.store.update(state)

    def get_workflow(self, workflow_id: UUID) -> Optional[WorkflowState]:
        return self.store.get(workflow_id)

    def _sync_graph_output_to_state(self, state: WorkflowState, output: dict[str, Any]) -> None:
        state.status = WorkflowStatus(output.get("status", state.status.value))
        state.current_step = output.get("current_step", state.current_step)
        state.diagnosis_summary = output.get("diagnosis_summary", state.diagnosis_summary)
        state.recommended_action = output.get("recommended_action", state.recommended_action)
        state.estimated_cost = output.get("estimated_cost", state.estimated_cost)
        state.requires_human_approval = output.get("requires_human_approval", state.requires_human_approval)
        state.human_approval_granted = output.get("human_approval_granted", state.human_approval_granted)
        state.structured_output = output.get("structured_output", state.structured_output)

        if output.get("error_details"):
            SafeFailureHandler.handle_safe_failure(
                state,
                error=output["error_details"],
                step_name=state.current_step
            )

        if state.status in (WorkflowStatus.Completed, WorkflowStatus.Failed, WorkflowStatus.Rejected):
            state.completed_at = datetime.now(timezone.utc)

        # Synchronize planned steps into state.steps
        if output.get("planned_steps"):
            existing_names = {s.step_name for s in state.steps}
            for p_step in output["planned_steps"]:
                if p_step["step_name"] not in existing_names:
                    state.steps.append(
                        WorkflowStepState(
                            step_name=p_step["step_name"],
                            step_order=len(state.steps) + 1,
                            status=WorkflowStepStatus.Pending,
                            summary=f"Delegated to {p_step['assigned_agent']}: {p_step['reason']}"
                        )
                    )

        # Record graph step
        state.steps.append(
            WorkflowStepState(
                step_name=state.current_step,
                step_order=len(state.steps) + 1,
                status=WorkflowStepStatus.Completed if state.status != WorkflowStatus.Failed else WorkflowStepStatus.Failed,
                summary=state.diagnosis_summary or state.current_step
            )
        )


# Global singleton engine instance
workflow_engine = WorkflowEngine()
