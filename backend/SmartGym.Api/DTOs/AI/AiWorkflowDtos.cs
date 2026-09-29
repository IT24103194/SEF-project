using System.Text.Json.Serialization;

namespace SmartGym.Api.DTOs.AI;

public class AiWorkflowStartRequestDto
{
    [JsonPropertyName("issue_id")]
    public Guid IssueId { get; set; }

    [JsonPropertyName("issue_title")]
    public string IssueTitle { get; set; } = string.Empty;

    [JsonPropertyName("equipment_id")]
    public Guid? EquipmentId { get; set; }

    [JsonPropertyName("equipment_name")]
    public string? EquipmentName { get; set; } = "Gym Equipment";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("workflow_type")]
    public string WorkflowType { get; set; } = "FacilityResolution";

    [JsonPropertyName("context_data")]
    public Dictionary<string, object> ContextData { get; set; } = new();

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }
}

public class AiWorkflowResumeRequestDto
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "approve"; // "approve", "reject", "revise"

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("revised_data")]
    public Dictionary<string, object>? RevisedData { get; set; }
}

public class AiWorkflowCancelRequestDto
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; } = "User requested cancellation";
}

public class AiToolExecutionRecordDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("input_parameters")]
    public Dictionary<string, object> InputParameters { get; set; } = new();

    [JsonPropertyName("output_result")]
    public object? OutputResult { get; set; }

    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; } = true;

    [JsonPropertyName("execution_time_ms")]
    public int ExecutionTimeMs { get; set; }

    [JsonPropertyName("executed_at")]
    public DateTime ExecutedAt { get; set; }
}

public class AiValidationResultRecordDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("rule_name")]
    public string RuleName { get; set; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("validation_message")]
    public string ValidationMessage { get; set; } = string.Empty;

    [JsonPropertyName("evaluated_at")]
    public DateTime EvaluatedAt { get; set; }
}

public class AiWorkflowStepResponseDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("step_name")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("step_order")]
    public int StepOrder { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("execution_duration_ms")]
    public int ExecutionDurationMs { get; set; }

    [JsonPropertyName("executed_at")]
    public DateTime ExecutedAt { get; set; }

    [JsonPropertyName("tool_executions")]
    public List<AiToolExecutionRecordDto> ToolExecutions { get; set; } = new();
}

public class AiWorkflowStateResponseDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("issue_id")]
    public Guid IssueId { get; set; }

    [JsonPropertyName("issue_title")]
    public string IssueTitle { get; set; } = string.Empty;

    [JsonPropertyName("equipment_name")]
    public string EquipmentName { get; set; } = string.Empty;

    [JsonPropertyName("workflow_type")]
    public string WorkflowType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("current_step")]
    public string CurrentStep { get; set; } = string.Empty;

    [JsonPropertyName("diagnosis_summary")]
    public string DiagnosisSummary { get; set; } = string.Empty;

    [JsonPropertyName("recommended_action")]
    public string RecommendedAction { get; set; } = string.Empty;

    [JsonPropertyName("estimated_confidence_score")]
    public double EstimatedConfidenceScore { get; set; }

    [JsonPropertyName("requires_human_approval")]
    public bool RequiresHumanApproval { get; set; }

    [JsonPropertyName("human_approval_granted")]
    public bool? HumanApprovalGranted { get; set; }

    [JsonPropertyName("estimated_cost")]
    public double? EstimatedCost { get; set; }

    [JsonPropertyName("total_tokens_used")]
    public int TotalTokensUsed { get; set; }

    [JsonPropertyName("model_identifier")]
    public string ModelIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("structured_output")]
    public Dictionary<string, object>? StructuredOutput { get; set; }

    [JsonPropertyName("steps")]
    public List<AiWorkflowStepResponseDto> Steps { get; set; } = new();

    [JsonPropertyName("validation_results")]
    public List<AiValidationResultRecordDto> ValidationResults { get; set; } = new();

    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("error_details")]
    public string? ErrorDetails { get; set; }
}

public class AiHealthResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
