using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Controllers;

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

[ApiController]
[Route("api/ai-workflows")]
[Produces("application/json")]
[Authorize]
public class AIWorkflowsController : ControllerBase
{
    private readonly SmartGymDbContext _context;

    public AIWorkflowsController(SmartGymDbContext context)
    {
        _context = context;
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
}
