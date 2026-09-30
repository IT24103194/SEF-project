from enum import Enum
from typing import Optional, Any
from uuid import UUID, uuid4
from pydantic import BaseModel, Field, field_validator, model_validator


class SupportedAgent(str, Enum):
    """
    Canonical specialized agents available in the SmartGym AI ecosystem.
    Planner must delegate only to these approved agents.
    """
    SAFETY_VALIDATION = "SafetyValidationAgent"
    DOMAIN_ANALYSIS = "DomainAnalysisAgent"
    INVENTORY = "InventoryAgent"
    SUPPLIER = "SupplierAgent"
    PROPOSAL = "ProposalAgent"
    APPROVAL_GATE = "ApprovalGateAgent"
    ACTION_EXECUTION = "ActionExecutionAgent"
    TICKET_UPDATE = "TicketUpdateAgent"


# Friendly aliases accepted from LLMs and normalized to canonical agent identifiers
AGENT_ALIASES: dict[str, SupportedAgent] = {
    # Canonical names
    "safetyvalidationagent": SupportedAgent.SAFETY_VALIDATION,
    "domainanalysisagent": SupportedAgent.DOMAIN_ANALYSIS,
    "inventoryagent": SupportedAgent.INVENTORY,
    "supplieragent": SupportedAgent.SUPPLIER,
    "proposalagent": SupportedAgent.PROPOSAL,
    "approvalgateagent": SupportedAgent.APPROVAL_GATE,
    "actionexecutionagent": SupportedAgent.ACTION_EXECUTION,
    "ticketupdateagent": SupportedAgent.TICKET_UPDATE,

    # Friendly human-readable aliases
    "safety & business validation agent": SupportedAgent.SAFETY_VALIDATION,
    "safety agent": SupportedAgent.SAFETY_VALIDATION,
    "validation agent": SupportedAgent.SAFETY_VALIDATION,
    "gym domain analysis agent": SupportedAgent.DOMAIN_ANALYSIS,
    "domain analysis agent": SupportedAgent.DOMAIN_ANALYSIS,
    "equipment diagnostic agent": SupportedAgent.DOMAIN_ANALYSIS,
    "inventory tool agent": SupportedAgent.INVENTORY,
    "inventory agent": SupportedAgent.INVENTORY,
    "supplier tool agent": SupportedAgent.SUPPLIER,
    "supplier agent": SupportedAgent.SUPPLIER,
    "repair proposal agent": SupportedAgent.PROPOSAL,
    "proposal agent": SupportedAgent.PROPOSAL,
    "authorization agent": SupportedAgent.APPROVAL_GATE,
    "human in the loop gate": SupportedAgent.APPROVAL_GATE,
    "approval agent": SupportedAgent.APPROVAL_GATE,
    "action / tool agent": SupportedAgent.ACTION_EXECUTION,
    "action agent": SupportedAgent.ACTION_EXECUTION,
    "tool execution agent": SupportedAgent.ACTION_EXECUTION,
    "ticket agent": SupportedAgent.TICKET_UPDATE,
    "ticket update agent": SupportedAgent.TICKET_UPDATE,
}


def normalize_agent_name(name: str) -> str:
    """
    Validates and normalizes an agent identifier.
    Raises ValueError if the agent is unsupported.
    """
    cleaned = (name or "").strip().lower()
    if cleaned in AGENT_ALIASES:
        return AGENT_ALIASES[cleaned].value
    raise ValueError(
        f"Unsupported agent assignment '{name}'. "
        f"Must be one of: {[a.value for a in SupportedAgent]}"
    )


class PlannerInput(BaseModel):
    """
    Input contract received by the Coordinator / Planner Agent.
    """
    workflow_id: UUID = Field(default_factory=uuid4)
    facility_issue_id: UUID
    objective: str = Field(min_length=5, description="The high-level domain objective to resolve")
    request_summary: str = Field(min_length=5, description="Summary of the incoming request and context")
    equipment_name: Optional[str] = "Gym Equipment"
    metadata: dict[str, Any] = Field(default_factory=dict)

    @field_validator("objective", "request_summary")
    @classmethod
    def validate_not_blank(cls, v: str, info) -> str:
        if not v or not v.strip():
            raise ValueError(f"'{info.field_name}' must not be empty or whitespace.")
        return v.strip()


class PlannedStep(BaseModel):
    """
    Individual step within a multi-step workflow plan.
    """
    step_order: int = Field(ge=1, description="1-based execution order index")
    step_name: str = Field(min_length=3, description="Descriptive name of the step")
    assigned_agent: str = Field(description="Target specialized agent responsible for execution")
    reason: str = Field(min_length=5, description="Why this step is necessary and why this agent was assigned")
    approval_required: bool = Field(default=False, description="Whether human approval is required before this step")
    status: str = Field(default="Pending", description="Execution status")

    @field_validator("step_name", "reason")
    @classmethod
    def validate_text(cls, v: str, info) -> str:
        if not v or not v.strip():
            raise ValueError(f"'{info.field_name}' cannot be empty or whitespace.")
        return v.strip()

    @field_validator("assigned_agent")
    @classmethod
    def validate_assigned_agent(cls, v: str) -> str:
        return normalize_agent_name(v)


class PlannerOutput(BaseModel):
    """
    Output contract produced by the Coordinator / Planner Agent.
    Schema validated against strict planning restrictions.
    """
    objective: str = Field(description="The domain objective being addressed")
    plan_id: UUID = Field(default_factory=uuid4, description="Unique plan identifier")
    steps: list[PlannedStep] = Field(min_length=1, description="Ordered execution steps")
    assigned_agent: str = Field(description="Initial or next agent assigned to begin execution")
    reason: str = Field(min_length=5, description="Overall planning justification")
    approval_required: bool = Field(description="Whether any step in the plan requires human authorization")

    @field_validator("objective", "reason")
    @classmethod
    def validate_text_fields(cls, v: str, info) -> str:
        if not v or not v.strip():
            raise ValueError(f"'{info.field_name}' cannot be empty or whitespace.")
        return v.strip()

    @field_validator("assigned_agent")
    @classmethod
    def validate_assigned_agent(cls, v: str) -> str:
        return normalize_agent_name(v)

    @model_validator(mode="after")
    def validate_plan_invariants(self) -> "PlannerOutput":
        # 1. Enforce step count
        if not self.steps:
            raise ValueError("PlannerOutput must contain at least one planned step.")

        # 2. Enforce sequential step ordering
        for idx, step in enumerate(self.steps, start=1):
            if step.step_order != idx:
                raise ValueError(
                    f"Invalid step order: step '{step.step_name}' has order {step.step_order}, expected {idx}."
                )

        # 3. Check consistency of approval_required
        has_approval_step = any(s.approval_required for s in self.steps)
        if has_approval_step and not self.approval_required:
            self.approval_required = True

        # 4. Strict Planner restrictions
        for step in self.steps:
            name_lower = step.step_name.lower()
            # Planner cannot bypass validation
            if "bypass" in name_lower and "validation" in name_lower:
                raise ValueError("Planner restriction violated: Cannot bypass validation.")
            # Planner cannot directly send vendor emails or approve repairs
            if "planner" in step.assigned_agent.lower():
                if any(kw in name_lower for kw in ["send email", "approve repair", "execute payment"]):
                    raise ValueError(
                        f"Planner restriction violated: Planner cannot execute high-impact action '{step.step_name}'."
                    )

        return self
