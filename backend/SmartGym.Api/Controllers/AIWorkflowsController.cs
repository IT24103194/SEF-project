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

[ApiController]
[Route("api/ai-workflows")]
[Produces("application/json")]
[Authorize]
public class AIWorkflowsController : ControllerBase
{
    private readonly SmartGymDbContext _context;
    private readonly IAiServiceClient? _aiServiceClient;
    private readonly ILogger<AIWorkflowsController>? _logger;

    public AIWorkflowsController(
        SmartGymDbContext context,
        IAiServiceClient? aiServiceClient = null,
        ILogger<AIWorkflowsController>? logger = null)
    {
        _context = context;
        _aiServiceClient = aiServiceClient;
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
            ro = new RepairOrder
            {
                IssueId = workflow.IssueId,
                EquipmentId = workflow.FacilityIssue.EquipmentId ?? Guid.Empty,
                OrderNumber = $"RO-AI-{DateTime.UtcNow:yyyyMMddHHmmss}",
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

        var approval = new Approval
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

        // 7. Workflow state update
        var oldStatus = workflow.Status;
        string approvalStatusStr;

        if (action == "APPROVE")
        {
            workflow.HumanApprovalGranted = true;
            workflow.Status = AIWorkflowStatus.Executing;
            workflow.CurrentStep = "Execution initiated following human approval";
            approvalStatusStr = "APPROVED";
            if (workflow.FacilityIssue != null)
            {
                workflow.FacilityIssue.Status = FacilityIssueStatus.APPROVED;
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
