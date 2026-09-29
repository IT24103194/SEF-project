from enum import Enum
from uuid import UUID, uuid4
from datetime import datetime, timezone
from typing import Optional, Any
from pydantic import BaseModel, Field


class WorkflowStatus(str, Enum):
    Initiated = "Initiated"
    Running = "Running"
    Executing = "Executing"
    AwaitingApproval = "AwaitingApproval"
    Approved = "Approved"
    Rejected = "Rejected"
    Completed = "Completed"
    Failed = "Failed"
    Cancelled = "Cancelled"


class WorkflowStepStatus(str, Enum):
    Pending = "Pending"
    Running = "Running"
    Completed = "Completed"
    Failed = "Failed"
    Skipped = "Skipped"


class ToolExecutionRecord(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    tool_name: str
    input_parameters: dict[str, Any] = Field(default_factory=dict)
    output_result: Optional[Any] = None
    is_success: bool = True
    execution_time_ms: int = 0
    executed_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))


class ValidationResultRecord(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    rule_name: str
    passed: bool
    validation_message: str
    evaluated_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))


class WorkflowStepState(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    step_name: str
    step_order: int
    status: WorkflowStepStatus = WorkflowStepStatus.Pending
    summary: str = ""
    execution_duration_ms: int = 0
    executed_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    tool_executions: list[ToolExecutionRecord] = Field(default_factory=list)


class WorkflowStartRequest(BaseModel):
    issue_id: UUID
    issue_title: str
    equipment_id: Optional[UUID] = None
    equipment_name: Optional[str] = "Gym Equipment"
    description: str
    workflow_type: str = "FacilityResolution"
    context_data: dict[str, Any] = Field(default_factory=dict)
    correlation_id: Optional[str] = None


class WorkflowResumeRequest(BaseModel):
    action: str = Field(default="approve", description="'approve', 'reject', or 'revise'")
    comments: Optional[str] = None
    revised_data: Optional[dict[str, Any]] = None


class WorkflowCancelRequest(BaseModel):
    reason: Optional[str] = "User requested cancellation"


class WorkflowState(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    issue_id: UUID
    issue_title: str = ""
    equipment_name: str = ""
    workflow_type: str = "FacilityResolution"
    status: WorkflowStatus = WorkflowStatus.Initiated
    current_step: str = "Initialization"
    diagnosis_summary: str = ""
    recommended_action: str = ""
    estimated_confidence_score: float = 0.0
    requires_human_approval: bool = False
    human_approval_granted: Optional[bool] = None
    estimated_cost: Optional[float] = None
    total_tokens_used: int = 0
    model_identifier: str = "smartgym-agentic-model"
    structured_output: Optional[dict[str, Any]] = None
    steps: list[WorkflowStepState] = Field(default_factory=list)
    validation_results: list[ValidationResultRecord] = Field(default_factory=list)
    correlation_id: str = ""
    started_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    completed_at: Optional[datetime] = None
    error_details: Optional[str] = None
