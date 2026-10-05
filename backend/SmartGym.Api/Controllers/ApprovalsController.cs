using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Authorization;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

public class ApprovalItemDto
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string IssueTitle { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public decimal ApprovalThreshold { get; set; }
    public string Decision { get; set; } = "Pending";
    public string? Comments { get; set; }
    public string? ManagerJustification { get; set; }
    public decimal? EstimatedBudgetImpact { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime RequestedAt { get; set; }
    public string EntityType { get; set; } = "RepairOrder";
    public Guid? AIWorkflowId { get; set; }
}

public class ApprovalDecisionRequest
{
    public string Decision { get; set; } = "Approved"; // "Approved", "Rejected", "RevisionRequired"
    public string? Comments { get; set; }
    public string? ManagerJustification { get; set; }
    public decimal? EstimatedBudgetImpact { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly SmartGymDbContext _context;
    private readonly INotificationService _notificationService;

    public ApprovalsController(SmartGymDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApprovalItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovals(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // 1. Repair orders needing approval or already evaluated
        var repairOrdersQuery = _context.RepairOrders
            .Include(ro => ro.Equipment)
            .Include(ro => ro.FacilityIssue)
                .ThenInclude(fi => fi.AIWorkflow)
            .Include(ro => ro.Approval)
                .ThenInclude(a => a!.Approver)
            .AsNoTracking();

        if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            repairOrdersQuery = repairOrdersQuery.Where(ro => ro.Status == RepairOrderStatus.PendingApproval || ro.Approval == null);
        }
        else if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            repairOrdersQuery = repairOrdersQuery.Where(ro => ro.Approval != null && ro.Approval.Decision == ApprovalDecision.Approved);
        }
        else if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            repairOrdersQuery = repairOrdersQuery.Where(ro => ro.Approval != null && ro.Approval.Decision == ApprovalDecision.Rejected);
        }

        var total = await repairOrdersQuery.CountAsync(cancellationToken);

        var list = await repairOrdersQuery
            .OrderByDescending(ro => ro.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ro => new ApprovalItemDto
            {
                Id = ro.Approval != null ? ro.Approval.Id : ro.Id,
                RepairOrderId = ro.Id,
                OrderNumber = ro.OrderNumber,
                EquipmentName = ro.Equipment.Name,
                IssueTitle = ro.FacilityIssue.Title,
                EstimatedCost = ro.EstimatedCost,
                ApprovalThreshold = ro.Approval != null ? ro.Approval.ApprovalThreshold : 25000.0m,
                Decision = ro.Approval != null ? ro.Approval.Decision.ToString() : (ro.Status == RepairOrderStatus.PendingApproval ? "Pending" : ro.Status.ToString()),
                Comments = ro.Approval != null ? ro.Approval.Comments : null,
                ApproverName = ro.Approval != null && ro.Approval.Approver != null ? $"{ro.Approval.Approver.FirstName} {ro.Approval.Approver.LastName}".Trim() : null,
                DecidedAt = ro.Approval != null ? ro.Approval.DecidedAt : null,
                RequestedAt = ro.CreatedAt,
                EntityType = "RepairOrder",
                AIWorkflowId = ro.FacilityIssue.AIWorkflow != null ? ro.FacilityIssue.AIWorkflow.Id : null
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ApprovalItemDto>
        {
            Items = list,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpPost("{id:guid}/decision")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ApprovalItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitDecision(
        Guid id,
        [FromBody] ApprovalDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var approverId = GetCurrentUserId();

        // Check if ID matches an Approval record or a RepairOrder
        var approval = await _context.Approvals
            .Include(a => a.RepairOrder)
                .ThenInclude(ro => ro.FacilityIssue)
                    .ThenInclude(fi => fi.AIWorkflow)
            .Include(a => a.RepairOrder.Equipment)
            .Include(a => a.Approver)
            .FirstOrDefaultAsync(a => a.Id == id || a.RepairOrderId == id, cancellationToken);

        RepairOrder? ro = null;
        if (approval != null)
        {
            ro = approval.RepairOrder;
        }
        else
        {
            ro = await _context.RepairOrders
                .Include(r => r.Equipment)
                .Include(r => r.FacilityIssue)
                    .ThenInclude(fi => fi.AIWorkflow)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        if (ro == null)
        {
            return NotFound(new { message = "Approval target not found." });
        }

        var isApproved = string.Equals(request.Decision, "Approved", StringComparison.OrdinalIgnoreCase);
        var decisionEnum = isApproved ? ApprovalDecision.Approved : ApprovalDecision.Rejected;

        // Business Rule: High-cost repairs (>= 25,000 LKR) require explicit justification for financial auditability
        if (isApproved && ro.EstimatedCost >= 25000.0m)
        {
            var combinedNotes = $"{request.ManagerJustification} {request.Comments}".Trim();
            if (string.IsNullOrWhiteSpace(combinedNotes) || combinedNotes.Length < 10)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Manager Justification Required",
                    Detail = "Repair orders exceeding the financial threshold of LKR 25,000 require an explicit manager justification note (minimum 10 characters)."
                });
            }
        }

        var finalComments = !string.IsNullOrWhiteSpace(request.ManagerJustification)
            ? $"[Justification: {request.ManagerJustification}] {request.Comments}".Trim()
            : request.Comments;

        if (approval == null)
        {
            approval = new Approval
            {
                RepairOrderId = ro.Id,
                ApproverUserId = approverId,
                Decision = decisionEnum,
                Comments = finalComments,
                DecidedAt = DateTime.UtcNow,
                EstimatedCost = ro.EstimatedCost,
                ApprovalThreshold = 25000.0m
            };
            await _context.Approvals.AddAsync(approval, cancellationToken);
        }
        else
        {
            approval.ApproverUserId = approverId;
            approval.Decision = decisionEnum;
            approval.Comments = finalComments;
            approval.DecidedAt = DateTime.UtcNow;
        }

        ro.Status = isApproved ? RepairOrderStatus.Approved : RepairOrderStatus.Rejected;
        if (ro.FacilityIssue != null)
        {
            ro.FacilityIssue.Status = isApproved ? FacilityIssueStatus.APPROVED : FacilityIssueStatus.REJECTED;
            if (ro.FacilityIssue.AIWorkflow != null)
            {
                ro.FacilityIssue.AIWorkflow.HumanApprovalGranted = isApproved;
                ro.FacilityIssue.AIWorkflow.Status = isApproved ? AIWorkflowStatus.Executing : AIWorkflowStatus.Failed;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send notification
        try
        {
            await _notificationService.NotifyRepairApprovalAsync(
                approverId,
                ro.Id,
                ro.Equipment?.Name ?? "Gym Equipment",
                ro.EstimatedCost,
                cancellationToken);
        }
        catch
        {
            // Non-fatal notification failure
        }

        return Ok(new ApprovalItemDto
        {
            Id = approval.Id,
            RepairOrderId = ro.Id,
            OrderNumber = ro.OrderNumber,
            EquipmentName = ro.Equipment != null ? ro.Equipment.Name : "Gym Equipment",
            IssueTitle = ro.FacilityIssue?.Title ?? "",
            EstimatedCost = ro.EstimatedCost,
            ApprovalThreshold = approval.ApprovalThreshold,
            Decision = approval.Decision.ToString(),
            Comments = approval.Comments,
            ManagerJustification = request.ManagerJustification,
            EstimatedBudgetImpact = request.EstimatedBudgetImpact ?? ro.EstimatedCost,
            ApproverName = "Staff Approver",
            DecidedAt = approval.DecidedAt,
            RequestedAt = ro.CreatedAt,
            EntityType = "RepairOrder",
            AIWorkflowId = ro.FacilityIssue?.AIWorkflow?.Id
        });
    }
}
