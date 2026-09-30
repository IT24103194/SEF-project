import json
from typing import Optional, Any
from uuid import UUID

from smartgym_ai.agents.base_agent import AgentBase
from smartgym_ai.models.workflow_models import WorkflowState
from smartgym_ai.models.planner_models import (
    PlannerInput,
    PlannerOutput,
    SupportedAgent,
    normalize_agent_name,
)
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.services.logging import ai_logger, get_correlation_id

PLANNER_SYSTEM_PROMPT = """You are the SmartGym Coordinator / Planner Agent.
Your responsibility is to analyze a domain objective and incoming request metadata,
then produce an authoritative, ordered, multi-step execution plan delegating each step
to the appropriate specialized agent.

PLANNER RESTRICTIONS (MANDATORY):
1. You are strictly a Coordinator / Planner. You CANNOT directly perform high-impact actions.
2. You CANNOT send vendor emails.
3. You CANNOT approve repairs.
4. You CANNOT directly execute repairs or financial transactions.
5. You CANNOT bypass safety or business validation.
6. You may ONLY access and analyze workflow metadata and request metadata.

SPECIALIZED AGENTS AVAILABLE FOR DELEGATION:
- SafetyValidationAgent: Validates safety risks, warranty constraints, and facility compliance.
- DomainAnalysisAgent: Inspects equipment telemetry, failure modes, and technical history.
- InventoryAgent: Queries gym stock, parts availability, and warehouse status.
- SupplierAgent: Identifies verified suppliers, lead times, and parts availability.
- ProposalAgent: Prepares detailed repair proposals and cost estimates.
- ApprovalGateAgent: Handles Human-In-The-Loop authorization when costs exceed thresholds.
- ActionExecutionAgent: Performs authorized physical interventions and technician work orders.
- TicketUpdateAgent: Closes or updates facility issue tickets with complete resolution notes.

STANDARD WORKFLOW PATTERN FOR FACILITY REPAIRS:
1. Validate facility issue (SafetyValidationAgent)
2. Analyze equipment history (DomainAnalysisAgent)
3. Check relevant inventory (InventoryAgent)
4. Identify suitable supplier (SupplierAgent)
5. Prepare repair proposal (ProposalAgent)
6. Validate proposal (SafetyValidationAgent)
7. Request authorization (ApprovalGateAgent - if approval required)
8. Execute approved action (ActionExecutionAgent)
9. Update ticket (TicketUpdateAgent)

OUTPUT REQUIREMENTS:
- You must return a structured JSON plan conforming strictly to the PlannerOutput schema.
- Every step must have a positive step_order starting at 1, a step_name, an assigned_agent from the allowed list, a clear reason, and approval_required boolean.
"""


class PlannerAgent(AgentBase):
    """
    Coordinator / Planner Agent implementation for SmartGym.
    Analyzes domain objectives and metadata, generating structured plans
    with appropriate delegation across specialized agents.
    """

    def __init__(self, llm_client: ILLMClient):
        super().__init__(
            name="PlannerAgent",
            role="Coordinator / Planner Agent",
            system_prompt=PLANNER_SYSTEM_PROMPT,
            llm_client=llm_client,
            tools=[]
        )

    async def create_plan(self, input_data: PlannerInput) -> PlannerOutput:
        """
        Generates and validates a structured execution plan for the given domain objective.
        Enforces schema validation and planner boundary constraints.
        """
        ai_logger.info(
            f"PlannerAgent generating plan for objective: '{input_data.objective}' "
            f"(Workflow ID: {input_data.workflow_id}, Issue ID: {input_data.facility_issue_id})",
            extra={
                "correlation_id": get_correlation_id(),
                "workflow_id": str(input_data.workflow_id),
                "extra_data": {
                    "objective": input_data.objective,
                    "equipment_name": input_data.equipment_name
                }
            }
        )

        user_content = (
            f"DOMAIN OBJECTIVE:\n{input_data.objective}\n\n"
            f"REQUEST SUMMARY:\n{input_data.request_summary}\n\n"
            f"EQUIPMENT:\n{input_data.equipment_name}\n\n"
            f"METADATA:\n{json.dumps(input_data.metadata, default=str)}\n\n"
            "Create a structured, complete workflow plan delegating each task to specialized agents."
        )

        messages = [
            {"role": "system", "content": self.system_prompt},
            {"role": "user", "content": user_content}
        ]

        # Use structured LLM client which enforces schema validation and retry/revision
        plan: PlannerOutput = await self.llm_client.generate_structured(
            messages=messages,
            schema=PlannerOutput,
            temperature=0.1
        )

        ai_logger.info(
            f"PlannerAgent successfully generated plan {plan.plan_id} with {len(plan.steps)} steps. "
            f"Initial assigned agent: '{plan.assigned_agent}', Approval required: {plan.approval_required}",
            extra={
                "correlation_id": get_correlation_id(),
                "workflow_id": str(input_data.workflow_id),
                "extra_data": {
                    "plan_id": str(plan.plan_id),
                    "total_steps": len(plan.steps),
                    "assigned_agent": plan.assigned_agent,
                    "approval_required": plan.approval_required
                }
            }
        )

        return plan

    async def _process(self, state: WorkflowState, context: dict[str, Any]) -> dict[str, Any]:
        """
        Implementation of AgentBase lifecycle for the LangGraph workflow engine.
        """
        objective = context.get("objective") or state.diagnosis_summary or f"Resolve facility issue for {state.equipment_name}"
        request_summary = context.get("request_summary") or state.issue_title or "Facility maintenance resolution request"

        planner_input = PlannerInput(
            workflow_id=state.id,
            facility_issue_id=state.issue_id,
            objective=objective,
            request_summary=request_summary,
            equipment_name=state.equipment_name,
            metadata=context.get("metadata", {})
        )

        plan = await self.create_plan(planner_input)

        first_step = plan.steps[0] if plan.steps else None
        next_step_name = first_step.step_name if first_step else "End"
        next_agent_name = first_step.assigned_agent if first_step else "None"

        return {
            "plan_id": str(plan.plan_id),
            "objective": plan.objective,
            "reason": plan.reason,
            "assigned_agent": plan.assigned_agent,
            "approval_required": plan.approval_required,
            "total_steps": len(plan.steps),
            "steps": [s.model_dump() for s in plan.steps],
            "next_step": next_step_name,
            "next_agent": next_agent_name,
            "summary": f"Plan generated with {len(plan.steps)} steps; next agent: {next_agent_name} for '{next_step_name}'."
        }
