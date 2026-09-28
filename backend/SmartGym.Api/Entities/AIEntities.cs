namespace SmartGym.Api.Entities;

public class AIWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssueId { get; set; }
    public FacilityIssue FacilityIssue { get; set; } = null!;

    public string WorkflowType { get; set; } = "FacilityIssueDiagnosis";
    public AIWorkflowStatus Status { get; set; } = AIWorkflowStatus.Initiated;
    public string CurrentStep { get; set; } = "Initiation";
    
    public string DiagnosisSummary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public double EstimatedConfidenceScore { get; set; } = 0.0;
    public bool RequiresHumanApproval { get; set; } = true;
    public bool? HumanApprovalGranted { get; set; }
    public int TotalTokensUsed { get; set; }
    public string ModelIdentifier { get; set; } = "gemini-1.5-pro";
    
    // Structured final payload without hidden chain-of-thought
    public string? StructuredOutputPayloadJson { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<AIWorkflowStep> Steps { get; set; } = new List<AIWorkflowStep>();
    public ICollection<AIValidationResult> ValidationResults { get; set; } = new List<AIValidationResult>();
}

public class AIWorkflowStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public AIWorkflow AIWorkflow { get; set; } = null!;

    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Status { get; set; } = "Completed";
    public string Summary { get; set; } = string.Empty;
    public long ExecutionDurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AIToolExecution> ToolExecutions { get; set; } = new List<AIToolExecution>();
}

public class AIToolExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowStepId { get; set; }
    public AIWorkflowStep WorkflowStep { get; set; } = null!;

    public string ToolName { get; set; } = string.Empty;
    public string InputParametersJson { get; set; } = "{}";
    public string OutputResultJson { get; set; } = "{}";
    public bool IsSuccess { get; set; } = true;
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class AIValidationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public AIWorkflow AIWorkflow { get; set; } = null!;

    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
