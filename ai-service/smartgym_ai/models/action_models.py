from enum import Enum
from typing import Optional, Any
from uuid import UUID
from pydantic import BaseModel, Field, ConfigDict, field_validator


class ApprovalStatus(str, Enum):
    PENDING = "PENDING"
    APPROVED = "APPROVED"
    REJECTED = "REJECTED"
    REVISION_REQUIRED = "REVISION_REQUIRED"


class ApprovalAction(str, Enum):
    APPROVE = "APPROVE"
    REJECT = "REJECT"
    REQUEST_REVISION = "REQUEST_REVISION"


class ActionAgentInput(BaseModel):
    """
    Input contract for the Action / Tool Agent.
    Contains validated domain recommendation, workflow metadata, and approval state.
    """
    domain_recommendation: dict[str, Any] = Field(default_factory=dict, alias="domainRecommendation", description="Output from DomainAnalysisAgent")
    workflow_context: dict[str, Any] = Field(default_factory=dict, alias="workflowContext", description="Metadata of the current workflow")
    approval_context: dict[str, Any] = Field(default_factory=dict, alias="approvalContext", description="Human approval decision and authorization details")

    model_config = ConfigDict(populate_by_name=True)

    @field_validator("domain_recommendation", "workflow_context", "approval_context")
    @classmethod
    def validate_dicts(cls, v: dict[str, Any]) -> dict[str, Any]:
        return v or {}


class ActionAgentOutput(BaseModel):
    """
    Output contract for the Action / Tool Agent.
    Specifies the concrete business actions prepared or executed.
    """
    proposed_action: str = Field(alias="proposedAction", description="Summary of the executed or proposed business action")
    repair_order_id: Optional[str] = Field(default=None, alias="repairOrderId", description="UUID of created or updated repair order")
    supplier_id: Optional[str] = Field(default=None, alias="supplierId", description="UUID of the selected equipment supplier")
    estimated_cost: float = Field(default=0.0, alias="estimatedCost", description="Authorized or estimated total repair cost")
    execution_plan: list[str] = Field(default_factory=list, alias="executionPlan", description="Step-by-step audit plan of executed actions")

    model_config = ConfigDict(populate_by_name=True)

    @field_validator("proposed_action")
    @classmethod
    def validate_proposed_action(cls, v: str) -> str:
        if not v or not str(v).strip():
            raise ValueError("'proposedAction' must not be empty.")
        return str(v).strip()

    @property
    def proposedAction(self) -> str:
        return self.proposed_action

    @property
    def repairOrderId(self) -> Optional[str]:
        return self.repair_order_id

    @property
    def supplierId(self) -> Optional[str]:
        return self.supplier_id

    @property
    def estimatedCost(self) -> float:
        return self.estimated_cost

    @property
    def executionPlan(self) -> list[str]:
        return self.execution_plan
