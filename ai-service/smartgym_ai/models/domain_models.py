from typing import Optional, Any
from uuid import UUID, uuid4
from pydantic import BaseModel, Field, ConfigDict, field_validator


class DomainAnalysisInput(BaseModel):
    """
    Input contract for the Gym Domain Analysis Agent.
    Contains facility issue details, equipment info, location, sanitized description,
    and maintenance context.
    """
    facility_issue: dict[str, Any] = Field(default_factory=dict)
    equipment: Optional[dict[str, Any]] = None
    location: Optional[dict[str, Any]] = None
    sanitized_description: str = Field(default="")
    maintenance_context: dict[str, Any] = Field(default_factory=dict)
    workflow_id: Optional[UUID] = None

    @field_validator("facility_issue", "maintenance_context")
    @classmethod
    def validate_dicts(cls, v: dict[str, Any]) -> dict[str, Any]:
        return v or {}


class DomainAnalysisOutput(BaseModel):
    """
    Structured domain recommendation produced by the Gym Domain Analysis Agent.
    Grounded in actual database queries and tool outputs.
    """
    possible_issue: str = Field(alias="possibleIssue", description="Technical diagnosis of the underlying root cause")
    priority_recommendation: str = Field(alias="priorityRecommendation", description="Recommended priority level (Low, Medium, High, Critical)")
    required_part: Optional[str] = Field(default=None, alias="requiredPart", description="Required replacement component, SKU, or part name")
    part_available: bool = Field(default=False, alias="partAvailable", description="Whether the required replacement part is in gym inventory stock")
    recommended_supplier: Optional[str] = Field(default=None, alias="recommendedSupplier", description="Supplier or service vendor recommended for the repair")
    estimated_cost: float = Field(alias="estimatedCost", description="Estimated total repair cost in LKR (or USD)")
    recommended_action: str = Field(alias="recommendedAction", description="Actionable recommendation for technicians or facility managers")
    supporting_data_references: list[str] = Field(
        default_factory=list,
        alias="supportingDataReferences",
        description="Observable references to actual data retrieved from tools (e.g. equipment serial, order numbers, SKU)"
    )

    model_config = ConfigDict(populate_by_name=True)

    @field_validator("possible_issue", "priority_recommendation", "recommended_action")
    @classmethod
    def validate_text_fields(cls, v: str, info) -> str:
        if not v or not str(v).strip():
            raise ValueError(f"'{info.field_name}' must not be empty.")
        return str(v).strip()

    @field_validator("estimated_cost")
    @classmethod
    def validate_cost(cls, v: float) -> float:
        if v < 0:
            raise ValueError("estimated_cost cannot be negative.")
        return float(v)
