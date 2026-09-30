from typing import Optional, Any
from uuid import UUID, uuid4
from pydantic import BaseModel, Field, ConfigDict, field_validator


class ContentModerationRecord(BaseModel):
    """Encapsulates text moderation details and sanitized output."""
    is_safe: bool = True
    original_text: str = ""
    sanitized_text: str = ""
    moderation_status: str = "Clean"  # "Clean" or "Flagged"
    moderation_reason: Optional[str] = None
    detected_keywords: list[str] = Field(default_factory=list)


class SafetyValidationInput(BaseModel):
    """
    Input contract received by the Safety, Content and Business Validation Agent.
    Combines facility issue metadata, planner output, and workflow context.
    """
    facility_issue: dict[str, Any] = Field(default_factory=dict)
    planner_output: Optional[dict[str, Any]] = None
    workflow_context: dict[str, Any] = Field(default_factory=dict)

    @field_validator("facility_issue", "workflow_context")
    @classmethod
    def validate_dicts(cls, v: dict[str, Any]) -> dict[str, Any]:
        return v or {}


class SafetyValidationOutput(BaseModel):
    """
    Authoritative output contract produced by the Safety & Business Validation Agent.
    Conforms to camelCase/snake_case for cross-service compatibility.
    """
    content_safe: bool = Field(alias="contentSafe", description="Whether content is free of offensive language and prompt injection")
    sanitized_description: str = Field(alias="sanitizedDescription", description="Sanitized description with inappropriate words masked")
    ticket_valid: bool = Field(alias="ticketValid", description="Whether the facility issue ticket structure and metadata are valid")
    business_rules_satisfied: bool = Field(alias="businessRulesSatisfied", description="Whether all deterministic business policies are satisfied")
    approval_required: bool = Field(alias="approvalRequired", description="Whether human approval is required based on cost or policy")
    issues: list[str] = Field(default_factory=list, description="List of detected safety, policy, or validation violations")
    validation_summary: str = Field(alias="validationSummary", description="Human-readable synthesis of validation findings")

    model_config = ConfigDict(populate_by_name=True)

    @field_validator("sanitized_description", "validation_summary")
    @classmethod
    def validate_strings(cls, v: str) -> str:
        return (v or "").strip()
