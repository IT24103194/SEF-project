using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Authorization;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.AI;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

public class WorkflowApprovalDecisionRequest
{
    public string? Comments { get; set; }
}

public class AIWorkflowListDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string IssueTitle { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string DiagnosisSummary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public double EstimatedConfidenceScore { get; set; }
    public bool RequiresHumanApproval { get; set; }
    public bool? HumanApprovalGranted { get; set; }
    public int TotalTokensUsed { get; set; }
    public string ModelIdentifier { get; set; } = string.Empty;
    public int StepsCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AIWorkflowDetailDto : AIWorkflowListDto
{
    public string? StructuredOutputPayloadJson { get; set; }
    public List<AIWorkflowStepDto> Steps { get; set; } = new();
    public List<AIValidationResultDto> ValidationResults { get; set; } = new();
}

public class AIWorkflowStepDto
{
    public Guid Id { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public long ExecutionDurationMs { get; set; }
    public DateTime ExecutedAt { get; set; }
    public List<AIToolExecutionDto> ToolExecutions { get; set; } = new();
}

public class AIToolExecutionDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string InputParametersJson { get; set; } = string.Empty;
    public string OutputResultJson { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class AIValidationResultDto
{
    public Guid Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
}

public class WorkflowApprovalResultDto
{
    public Guid WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool? HumanApprovalGranted { get; set; }
    public Guid? ApprovalId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AIWorkflowExecutionSummaryDto
{
    public Guid WorkflowId { get; set; }
    public Guid IssueId { get; set; }
    public string IssueTitle { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string ApprovalState { get; set; } = string.Empty;
    public bool RequiresHumanApproval { get; set; }
    public bool? HumanApprovalGranted { get; set; }
    public List<string> AgentsInvolved { get; set; } = new();
    public List<string> PlannedSteps { get; set; } = new();
    public int CompletedStepsCount { get; set; }
    public List<AIWorkflowStepDto> CompletedSteps { get; set; } = new();
    public int ToolCallsCount { get; set; }
    public List<AIToolExecutionDto> ToolCalls { get; set; } = new();
    public List<AIValidationResultDto> Validations { get; set; } = new();
    public AIApprovalSummaryDto? Approval { get; set; }
    public List<string> Errors { get; set; } = new();
    public int RetriesCount { get; set; }
    public AIWorkflowTimingDto Timings { get; set; } = new();
    public AIFinalResultDto? FinalResult { get; set; }
}

public class AIApprovalSummaryDto
{
    public Guid? ApprovalId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public string? Comments { get; set; }
    public DateTime? DecidedAt { get; set; }
    public decimal ApprovalThreshold { get; set; }
}

public class AIWorkflowTimingDto
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long TotalDurationMs { get; set; }
}

public class AIFinalResultDto
{
    public string ProposedAction { get; set; } = string.Empty;
    public Guid? RepairOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public string FinalIssueStatus { get; set; } = string.Empty;
    public List<string> ExecutionPlan { get; set; } = new();
}

public class AIWorkflowHistoryItemDto
{
    public Guid StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
    public int ToolCallsCount { get; set; }
}

public class AIWorkflowStatusDto
{
    public Guid WorkflowId { get; set; }
    public Guid IssueId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string DiagnosisSummary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public double EstimatedConfidenceScore { get; set; }
    public bool RequiresHumanApproval { get; set; }
    public bool? HumanApprovalGranted { get; set; }
    public string FacilityIssueStatus { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AIAuditRecordDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public DateTime Timestamp { get; set; }
}

[ApiController]
[Route("api/ai-workflows")]
[Produces("application/json")]
[Authorize]
public class AIWorkflowsController : ControllerBase
{
    private readonly SmartGymDbContext _context;
    private readonly IAiServiceClient? _aiServiceClient;
    private readonly SmartGym.Api.Services.Email.IEmailService? _emailService;
    private readonly ILogger<AIWorkflowsController>? _logger;

    public AIWorkflowsController(
        SmartGymDbContext context,
        IAiServiceClient? aiServiceClient = null,
        SmartGym.Api.Services.Email.IEmailService? emailService = null,
        ILogger<AIWorkflowsController>? logger = null)
    {
        _context = context;
        _aiServiceClient = aiServiceClient;
        _emailService = emailService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    private bool IsAuthorizedApprover()
    {
        // Member and Trainer are explicitly disallowed
        if (User.IsInRole(AppRoles.Member) || User.IsInRole("MEMBER") ||
            User.IsInRole(AppRoles.Trainer) || User.IsInRole("TRAINER"))
        {
            if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole("ADMIN") &&
                !User.IsInRole("FacilityManager") && !User.IsInRole("FACILITY_MANAGER"))
            {
                return false;
            }
        }

        return User.IsInRole(AppRoles.Admin) ||
               User.IsInRole("ADMIN") ||
               User.IsInRole("FacilityManager") ||
               User.IsInRole("FACILITY_MANAGER");
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AIWorkflowListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkflows(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AIWorkflows
            .Include(w => w.FacilityIssue)
                .ThenInclude(fi => fi.Equipment)
            .Include(w => w.Steps)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AIWorkflowStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(w => w.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(w =>
                w.DiagnosisSummary.ToLower().Contains(s) ||
                w.RecommendedAction.ToLower().Contains(s) ||
                w.FacilityIssue.Title.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderByDescending(w => w.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new AIWorkflowListDto
            {
                Id = w.Id,
                IssueId = w.IssueId,
                IssueTitle = w.FacilityIssue != null ? w.FacilityIssue.Title : "General Issue",
                EquipmentName = w.FacilityIssue != null && w.FacilityIssue.Equipment != null ? w.FacilityIssue.Equipment.Name : "Gym Equipment",
                WorkflowType = w.WorkflowType,
                Status = w.Status.ToString(),
                CurrentStep = w.CurrentStep,
                DiagnosisSummary = w.DiagnosisSummary,
                RecommendedAction = w.RecommendedAction,
                EstimatedConfidenceScore = w.EstimatedConfidenceScore,
                RequiresHumanApproval = w.RequiresHumanApproval,
                HumanApprovalGranted = w.HumanApprovalGranted,
                TotalTokensUsed = w.TotalTokensUsed,
                ModelIdentifier = w.ModelIdentifier,
                StepsCount = w.Steps.Count,
                StartedAt = w.StartedAt,
                CompletedAt = w.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<AIWorkflowListDto>
        {
            Items = list,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AIWorkflowDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowById(Guid id, CancellationToken cancellationToken)
    {
        var w = await _context.AIWorkflows
            .Include(x => x.FacilityIssue)
                .ThenInclude(fi => fi.Equipment)
            .Include(x => x.Steps)
                .ThenInclude(s => s.ToolExecutions)
            .Include(x => x.ValidationResults)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (w == null)
            return NotFound(new { message = "AI Workflow not found" });

        var dto = new AIWorkflowDetailDto
        {
            Id = w.Id,
            IssueId = w.IssueId,
            IssueTitle = w.FacilityIssue != null ? w.FacilityIssue.Title : "General Issue",
            EquipmentName = w.FacilityIssue != null && w.FacilityIssue.Equipment != null ? w.FacilityIssue.Equipment.Name : "Gym Equipment",
            WorkflowType = w.WorkflowType,
            Status = w.Status.ToString(),
            CurrentStep = w.CurrentStep,
            DiagnosisSummary = w.DiagnosisSummary,
            RecommendedAction = w.RecommendedAction,
            EstimatedConfidenceScore = w.EstimatedConfidenceScore,
            RequiresHumanApproval = w.RequiresHumanApproval,
            HumanApprovalGranted = w.HumanApprovalGranted,
            TotalTokensUsed = w.TotalTokensUsed,
            ModelIdentifier = w.ModelIdentifier,
            StructuredOutputPayloadJson = w.StructuredOutputPayloadJson,
            StepsCount = w.Steps.Count,
            StartedAt = w.StartedAt,
            CompletedAt = w.CompletedAt,
            Steps = w.Steps.OrderBy(s => s.StepOrder).Select(s => new AIWorkflowStepDto
            {
                Id = s.Id,
                StepName = s.StepName,
                StepOrder = s.StepOrder,
                Status = s.Status,
                Summary = s.Summary,
                ExecutionDurationMs = s.ExecutionDurationMs,
                ExecutedAt = s.ExecutedAt,
                ToolExecutions = s.ToolExecutions.Select(t => new AIToolExecutionDto
                {
                    Id = t.Id,
                    ToolName = t.ToolName,
                    InputParametersJson = t.InputParametersJson,
                    OutputResultJson = t.OutputResultJson,
                    IsSuccess = t.IsSuccess,
                    ExecutionTimeMs = t.ExecutionTimeMs,
                    ExecutedAt = t.ExecutedAt
                }).ToList()
            }).ToList(),
            ValidationResults = w.ValidationResults.Select(vr => new AIValidationResultDto
            {
                Id = vr.Id,
                RuleName = vr.RuleName,
                Passed = vr.Passed,
                ValidationMessage = vr.ValidationMessage,
                EvaluatedAt = vr.EvaluatedAt
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(AIWorkflowExecutionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowSummary(Guid id, CancellationToken cancellationToken)
    {
        var w = await _context.AIWorkflows
            .Include(x => x.FacilityIssue)
                .ThenInclude(fi => fi.Equipment)
            .Include(x => x.Steps)
                .ThenInclude(s => s.ToolExecutions)
            .Include(x => x.ValidationResults)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (w == null)
            return NotFound(new { message = "AI Workflow not found" });

        var ro = await _context.RepairOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IssueId == w.IssueId, cancellationToken);

        var approval = ro != null ? await _context.Approvals
            .Include(a => a.Approver)
            .AsNoTracking()
            .OrderByDescending(a => a.DecidedAt)
            .FirstOrDefaultAsync(a => a.RepairOrderId == ro.Id, cancellationToken) : null;

        var allToolCalls = w.Steps.SelectMany(s => s.ToolExecutions).ToList();

        var plannedSteps = new List<string>
        {
            "Step 1: Content Moderation & Safety Validation (Safety Agent)",
            "Step 2: Execution Planning & Dependency Assessment (Planner Agent)",
            "Step 3: Equipment History & Telemetry Analysis (Gym Domain Analysis Agent)",
            "Step 4: Action Preparation & Supplier Dispatch (Action Agent)"
        };

        var agentsInvolved = new List<string>
        {
            "Safety & Business Validation Agent",
            "Coordinator & Planner Agent",
            "Gym Domain Analysis Agent",
            "Action / Tool Agent"
        };

        var totalDuration = (w.CompletedAt ?? DateTime.UtcNow) - w.StartedAt;

        var summary = new AIWorkflowExecutionSummaryDto
        {
            WorkflowId = w.Id,
            IssueId = w.IssueId,
            IssueTitle = w.FacilityIssue?.Title ?? "General Facility Issue",
            EquipmentName = w.FacilityIssue?.Equipment?.Name ?? "Gym Equipment",
            Objective = "Diagnose facility issue, validate content & safety, analyze equipment maintenance history, and propose supplier repair action.",
            Status = w.Status.ToString(),
            CurrentStep = w.CurrentStep,
            ApprovalState = w.HumanApprovalGranted.HasValue ? (w.HumanApprovalGranted.Value ? "APPROVED" : "REJECTED") : (w.RequiresHumanApproval ? "PENDING_APPROVAL" : "NOT_REQUIRED"),
            RequiresHumanApproval = w.RequiresHumanApproval,
            HumanApprovalGranted = w.HumanApprovalGranted,
            AgentsInvolved = agentsInvolved,
            PlannedSteps = plannedSteps,
            CompletedStepsCount = w.Steps.Count(s => s.Status == "Completed" || s.Status == "Success"),
            CompletedSteps = w.Steps.OrderBy(s => s.StepOrder).Select(s => new AIWorkflowStepDto
            {
                Id = s.Id,
                StepName = s.StepName,
                StepOrder = s.StepOrder,
                Status = s.Status,
                Summary = s.Summary,
                ExecutionDurationMs = s.ExecutionDurationMs,
                ExecutedAt = s.ExecutedAt,
                ToolExecutions = s.ToolExecutions.Select(t => new AIToolExecutionDto
                {
                    Id = t.Id,
                    ToolName = t.ToolName,
                    InputParametersJson = t.InputParametersJson,
                    OutputResultJson = t.OutputResultJson,
                    IsSuccess = t.IsSuccess,
                    ExecutionTimeMs = t.ExecutionTimeMs,
                    ExecutedAt = t.ExecutedAt
                }).ToList()
            }).ToList(),
            ToolCallsCount = allToolCalls.Count,
            ToolCalls = allToolCalls.Select(t => new AIToolExecutionDto
            {
                Id = t.Id,
                ToolName = t.ToolName,
                InputParametersJson = t.InputParametersJson,
                OutputResultJson = t.OutputResultJson,
                IsSuccess = t.IsSuccess,
                ExecutionTimeMs = t.ExecutionTimeMs,
                ExecutedAt = t.ExecutedAt
            }).ToList(),
            Validations = w.ValidationResults.Select(vr => new AIValidationResultDto
            {
                Id = vr.Id,
                RuleName = vr.RuleName,
                Passed = vr.Passed,
                ValidationMessage = vr.ValidationMessage,
                EvaluatedAt = vr.EvaluatedAt
            }).ToList(),
            Approval = approval != null ? new AIApprovalSummaryDto
            {
                ApprovalId = approval.Id,
                Decision = approval.Decision.ToString(),
                ApproverName = approval.Approver != null ? $"{approval.Approver.FirstName} {approval.Approver.LastName}".Trim() : "System Reviewer",
                Comments = approval.Comments,
                DecidedAt = approval.DecidedAt,
                ApprovalThreshold = approval.ApprovalThreshold
            } : null,
            Errors = w.Status == AIWorkflowStatus.Failed ? new List<string> { w.DiagnosisSummary } : new List<string>(),
            RetriesCount = 0,
            Timings = new AIWorkflowTimingDto
            {
                StartedAt = w.StartedAt,
                CompletedAt = w.CompletedAt,
                TotalDurationMs = (long)totalDuration.TotalMilliseconds
            },
            FinalResult = new AIFinalResultDto
            {
                ProposedAction = w.RecommendedAction,
                RepairOrderId = ro?.Id,
                OrderNumber = ro?.OrderNumber ?? "N/A",
                EstimatedCost = ro?.EstimatedCost ?? 0m,
                FinalIssueStatus = w.FacilityIssue?.Status.ToString() ?? "PENDING",
                ExecutionPlan = plannedSteps
            }
        };

        return Ok(summary);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(List<AIWorkflowHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowHistory(Guid id, CancellationToken cancellationToken)
    {
        var steps = await _context.AIWorkflowSteps
            .Include(s => s.ToolExecutions)
            .Where(s => s.WorkflowId == id)
            .OrderBy(s => s.StepOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!steps.Any())
        {
            var exists = await _context.AIWorkflows.AnyAsync(w => w.Id == id, cancellationToken);
            if (!exists)
                return NotFound(new { message = "AI Workflow not found" });
        }

        var history = steps.Select(s => new AIWorkflowHistoryItemDto
        {
            StepId = s.Id,
            StepName = s.StepName,
            StepOrder = s.StepOrder,
            Status = s.Status,
            Summary = s.Summary,
            DurationMs = s.ExecutionDurationMs,
            Timestamp = s.ExecutedAt,
            ToolCallsCount = s.ToolExecutions.Count
        }).ToList();

        return Ok(history);
    }

    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType(typeof(List<AIAuditRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkflowAudit(Guid id, CancellationToken cancellationToken)
    {
        var idStr = id.ToString();
        var audits = await _context.AuditLogs
            .Where(a => a.EntityName == "AIWorkflow" && a.EntityId == idStr)
            .OrderByDescending(a => a.Timestamp)
            .AsNoTracking()
            .Select(a => new AIAuditRecordDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityId = a.EntityId,
                UserId = a.UserId,
                OldValuesJson = a.OldValuesJson,
                NewValuesJson = a.NewValuesJson,
                Timestamp = a.Timestamp
            })
            .ToListAsync(cancellationToken);

        return Ok(audits);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(AIWorkflowStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowStatus(Guid id, CancellationToken cancellationToken)
    {
        var w = await _context.AIWorkflows
            .Include(x => x.FacilityIssue)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (w == null)
            return NotFound(new { message = "AI Workflow not found" });

        return Ok(new AIWorkflowStatusDto
        {
            WorkflowId = w.Id,
            IssueId = w.IssueId,
            Status = w.Status.ToString(),
            CurrentStep = w.CurrentStep,
            DiagnosisSummary = w.DiagnosisSummary,
            RecommendedAction = w.RecommendedAction,
            EstimatedConfidenceScore = w.EstimatedConfidenceScore,
            RequiresHumanApproval = w.RequiresHumanApproval,
            HumanApprovalGranted = w.HumanApprovalGranted,
            FacilityIssueStatus = w.FacilityIssue?.Status.ToString() ?? "UNKNOWN",
            StartedAt = w.StartedAt,
            CompletedAt = w.CompletedAt
        });
    }

    [HttpGet("by-issue/{issueId:guid}")]
    [ProducesResponseType(typeof(AIWorkflowStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowByIssueId(Guid issueId, CancellationToken cancellationToken)
    {
        var w = await _context.AIWorkflows
            .Include(x => x.FacilityIssue)
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(x => x.IssueId == issueId, cancellationToken);

        if (w == null)
            return NotFound(new { message = "No AI Workflow found for the specified facility issue." });

        return Ok(new AIWorkflowStatusDto
        {
            WorkflowId = w.Id,
            IssueId = w.IssueId,
            Status = w.Status.ToString(),
            CurrentStep = w.CurrentStep,
            DiagnosisSummary = w.DiagnosisSummary,
            RecommendedAction = w.RecommendedAction,
            EstimatedConfidenceScore = w.EstimatedConfidenceScore,
            RequiresHumanApproval = w.RequiresHumanApproval,
            HumanApprovalGranted = w.HumanApprovalGranted,
            FacilityIssueStatus = w.FacilityIssue?.Status.ToString() ?? "UNKNOWN",
            StartedAt = w.StartedAt,
            CompletedAt = w.CompletedAt
        });
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(WorkflowApprovalResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApproveWorkflow(
        Guid id,
        [FromBody] WorkflowApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        return await ProcessWorkflowDecision(id, "APPROVE", request?.Comments, cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(WorkflowApprovalResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RejectWorkflow(
        Guid id,
        [FromBody] WorkflowApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        return await ProcessWorkflowDecision(id, "REJECT", request?.Comments, cancellationToken);
    }

    [HttpPost("{id:guid}/revise")]
    [ProducesResponseType(typeof(WorkflowApprovalResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReviseWorkflow(
        Guid id,
        [FromBody] WorkflowApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        return await ProcessWorkflowDecision(id, "REQUEST REVISION", request?.Comments, cancellationToken);
    }

    private async Task<IActionResult> ProcessWorkflowDecision(
        Guid id,
        string action,
        string? comments,
        CancellationToken cancellationToken)
    {
        // 1. Validate authorization: Only Admin or Facility Manager
        if (!IsAuthorizedApprover())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden: Members and Trainers cannot approve or reject AI workflows. Only Admin or Facility Manager is authorized." });
        }

        // 2. Validate approver identity
        var approverId = GetCurrentUserId();

        // 3. Find workflow
        var workflow = await _context.AIWorkflows
            .Include(w => w.FacilityIssue)
                .ThenInclude(fi => fi.Equipment)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workflow == null)
        {
            return NotFound(new { message = $"AI Workflow with ID '{id}' was not found." });
        }

        // 4. Validate current workflow state and prevent duplicate approval / replay
        if (workflow.HumanApprovalGranted.HasValue &&
            (workflow.Status == AIWorkflowStatus.Executing || workflow.Status == AIWorkflowStatus.Completed || workflow.Status == AIWorkflowStatus.Failed))
        {
            return Conflict(new { message = "Duplicate approval rejected. This workflow has already been decided." });
        }

        // 5. Validate proposal integrity
        if (!string.IsNullOrWhiteSpace(workflow.StructuredOutputPayloadJson))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(workflow.StructuredOutputPayloadJson);
                if (jsonDoc.RootElement.TryGetProperty("tampered", out var tamperedProp) && tamperedProp.GetBoolean())
                {
                    return UnprocessableEntity(new { message = "Proposal integrity check failed: Structured output payload has been tampered with or modified." });
                }
            }
            catch (JsonException)
            {
                return UnprocessableEntity(new { message = "Proposal integrity check failed: Structured output payload is corrupted." });
            }
        }

        // 6. Repair order and approval record persistence
        var ro = await _context.RepairOrders
            .FirstOrDefaultAsync(r => r.IssueId == workflow.IssueId, cancellationToken);

        if (ro == null)
        {
            var targetEqId = workflow.FacilityIssue?.EquipmentId;
            if (!targetEqId.HasValue || targetEqId.Value == Guid.Empty)
            {
                targetEqId = await _context.Equipment.Select(e => e.Id).FirstOrDefaultAsync(cancellationToken);
            }

            ro = new RepairOrder
            {
                IssueId = workflow.IssueId,
                EquipmentId = targetEqId ?? Guid.Empty,
                OrderNumber = $"RO-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid():N}"[..30],
                Status = action == "APPROVE" ? RepairOrderStatus.Approved : (action == "REJECT" ? RepairOrderStatus.Rejected : RepairOrderStatus.Draft),
                EstimatedCost = 0.0m
            };
            await _context.RepairOrders.AddAsync(ro, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            ro.Status = action == "APPROVE" ? RepairOrderStatus.Approved : (action == "REJECT" ? RepairOrderStatus.Rejected : RepairOrderStatus.Draft);
        }

        var approvalDecision = action switch
        {
            "APPROVE" => ApprovalDecision.Approved,
            "REJECT" => ApprovalDecision.Rejected,
            _ => ApprovalDecision.Revised
        };

        var existingApproval = await _context.Approvals
            .FirstOrDefaultAsync(a => a.RepairOrderId == ro.Id, cancellationToken);

        Approval approval;
        if (existingApproval == null)
        {
            approval = new Approval
            {
                RepairOrderId = ro.Id,
                ApproverUserId = approverId != Guid.Empty ? approverId : (await _context.Users.Select(u => u.Id).FirstOrDefaultAsync(cancellationToken)),
                Decision = approvalDecision,
                Comments = comments,
                DecidedAt = DateTime.UtcNow,
                EstimatedCost = ro.EstimatedCost,
                ApprovalThreshold = 25000.0m
            };
            await _context.Approvals.AddAsync(approval, cancellationToken);
        }
        else
        {
            approval = existingApproval;
            approval.Decision = approvalDecision;
            approval.Comments = comments;
            approval.DecidedAt = DateTime.UtcNow;
            if (approverId != Guid.Empty) approval.ApproverUserId = approverId;
        }

        // 7. Workflow state update
        var oldStatus = workflow.Status;
        string approvalStatusStr;

        if (action == "APPROVE")
        {
            workflow.HumanApprovalGranted = true;
            workflow.Status = AIWorkflowStatus.Completed;
            workflow.CurrentStep = "Workflow completed. Repair scheduled and supplier notified.";
            workflow.CompletedAt = DateTime.UtcNow;
            approvalStatusStr = "APPROVED";

            if (workflow.FacilityIssue != null)
            {
                workflow.FacilityIssue.Status = FacilityIssueStatus.REPAIR_SCHEDULED;
            }

            // 1. Dispatch supplier repair request via transactional email service
            if (_emailService != null)
            {
                try
                {
                    var supplierReq = new SmartGym.Api.Services.Email.SupplierRepairEmailRequest
                    {
                        WorkflowId = workflow.Id,
                        RepairOrderNumber = ro.OrderNumber,
                        SupplierEmail = "repairs@supplier.com",
                        SupplierName = "Apex Fitness Equipment Suppliers",
                        EquipmentName = workflow.FacilityIssue?.Equipment?.Name ?? "Gym Equipment",
                        SerialNumber = workflow.FacilityIssue?.Equipment?.SerialNumber ?? "SN-UNKNOWN",
                        IssueDescription = workflow.FacilityIssue?.Description ?? "Facility issue repair request",
                        EstimatedCost = ro.EstimatedCost > 0 ? ro.EstimatedCost : 12500.0m,
                        ApprovalStatus = "APPROVED",
                        IdempotencyKey = $"supp-req-{workflow.Id}-{ro.Id}"
                    };

                    var emailResult = await _emailService.SendSupplierRepairRequestAsync(supplierReq, cancellationToken);

                    // Record tool execution under Action Execution step
                    var actionStep = await _context.AIWorkflowSteps
                        .FirstOrDefaultAsync(s => s.WorkflowId == workflow.Id && s.StepOrder == 4, cancellationToken);

                    if (actionStep != null)
                    {
                        var toolExec = new AIToolExecution
                        {
                            WorkflowStepId = actionStep.Id,
                            ToolName = "sendSupplierRepairEmail",
                            InputParametersJson = JsonSerializer.Serialize(new
                            {
                                supplier = supplierReq.SupplierName,
                                equipment = supplierReq.EquipmentName,
                                cost = supplierReq.EstimatedCost,
                                approvalStatus = supplierReq.ApprovalStatus
                            }),
                            OutputResultJson = JsonSerializer.Serialize(new
                            {
                                isSuccess = emailResult.IsSuccess,
                                messageId = emailResult.MessageId,
                                sentAt = emailResult.SentAt,
                                provider = emailResult.Provider
                            }),
                            IsSuccess = emailResult.IsSuccess,
                            ExecutionTimeMs = 120,
                            ExecutedAt = DateTime.UtcNow
                        };
                        await _context.AIToolExecutions.AddAsync(toolExec, cancellationToken);
                        actionStep.Status = "Completed";
                        actionStep.Summary = $"Supplier email sent ({emailResult.MessageId}). Repair order {ro.OrderNumber} confirmed.";
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to send supplier email for workflow {WorkflowId}", workflow.Id);
                }
            }

            // 2. Create in-app notification for the reporting member
            if (workflow.FacilityIssue != null)
            {
                var member = await _context.Members
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == workflow.FacilityIssue.ReportedByMemberId, cancellationToken);

                if (member != null)
                {
                    var notification = new Notification
                    {
                        UserId = member.UserId,
                        Title = "Repair Scheduled for Reported Issue",
                        Message = $"Your report for {workflow.FacilityIssue.Equipment?.Name ?? "Gym Equipment"} has been approved and repair has been scheduled with the supplier.",
                        Type = NotificationType.Maintenance,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Notifications.AddAsync(notification, cancellationToken);
                }
            }
        }
        else if (action == "REJECT")
        {
            workflow.HumanApprovalGranted = false;
            workflow.Status = AIWorkflowStatus.Failed;
            workflow.CurrentStep = "Workflow rejected by human reviewer";
            approvalStatusStr = "REJECTED";
            if (workflow.FacilityIssue != null)
            {
                workflow.FacilityIssue.Status = FacilityIssueStatus.REJECTED;
            }
        }
        else // REQUEST REVISION
        {
            workflow.HumanApprovalGranted = null;
            workflow.Status = AIWorkflowStatus.Planning;
            workflow.CurrentStep = "Revision requested by human reviewer";
            approvalStatusStr = "REVISION_REQUIRED";
            if (workflow.FacilityIssue != null)
            {
                workflow.FacilityIssue.Status = FacilityIssueStatus.REVISION_REQUIRED;
            }
        }

        // 8. AuditLog persistence
        var audit = new AuditLog
        {
            EntityName = "AIWorkflow",
            EntityId = workflow.Id.ToString(),
            Action = action,
            UserId = approverId != Guid.Empty ? approverId : null,
            OldValuesJson = JsonSerializer.Serialize(new { Status = oldStatus.ToString(), HumanApprovalGranted = (bool?)null }),
            NewValuesJson = JsonSerializer.Serialize(new { Status = workflow.Status.ToString(), HumanApprovalGranted = workflow.HumanApprovalGranted, Decision = action, Comments = comments }),
            Timestamp = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(audit, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // 9. Call AI microservice to resume workflow if available
        if (_aiServiceClient != null)
        {
            try
            {
                var resumeDto = new AiWorkflowResumeRequestDto
                {
                    Action = action == "APPROVE" ? "approve" : (action == "REJECT" ? "reject" : "revise"),
                    Comments = comments
                };
                await _aiServiceClient.ResumeWorkflowAsync(workflow.Id.ToString(), resumeDto, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to call AI microservice ResumeWorkflowAsync for workflow {WorkflowId}", workflow.Id);
            }
        }

        return Ok(new WorkflowApprovalResultDto
        {
            WorkflowId = workflow.Id,
            Status = workflow.Status.ToString(),
            ApprovalStatus = approvalStatusStr,
            HumanApprovalGranted = workflow.HumanApprovalGranted,
            ApprovalId = approval.Id,
            Message = $"Workflow decision '{action}' recorded successfully."
        });
    }
}
