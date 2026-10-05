using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/facility-issues")]
[Produces("application/json")]
public class FacilityIssuesController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly ILogger<FacilityIssuesController> _logger;

    public FacilityIssuesController(IFacilityService facilityService, ILogger<FacilityIssuesController> logger)
    {
        _facilityService = facilityService;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to extract the authenticated user's ID and roles from their JWT claims.
    /// </summary>
    /// <returns>A tuple containing the User ID as a Guid and a list of assigned roles.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user identity is missing or invalid.</exception>
    private (Guid UserId, IList<string> Roles) GetCurrentUserContext()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user identity in token.");
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        return (userId, roles);
    }

    /// <summary>
    /// Retrieve paginated facility issues. Members see only their own issues; Staff/Admin see all issues.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<FacilityIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilityIssues(
        [FromQuery] FacilityIssueQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.GetFacilityIssuesAsync(parameters, userId, roles, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve facility issue details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(FacilityIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFacilityIssueById(Guid id, CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.GetFacilityIssueByIdAsync(id, userId, roles, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Report a new equipment or facility issue. Supports deterministic content moderation.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(FacilityIssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFacilityIssue(
        [FromBody] CreateFacilityIssueRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = GetCurrentUserContext();
        var result = await _facilityService.CreateFacilityIssueAsync(request, userId, null, cancellationToken);
        return CreatedAtAction(nameof(GetFacilityIssueById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Report a new equipment or facility issue with an attached photo (multipart/form-data).
    /// </summary>
    [HttpPost("with-image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FacilityIssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFacilityIssueWithImage(
        [FromForm] Guid locationId,
        [FromForm] Guid? equipmentId,
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] IssueSeverity severity,
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        var (userId, _) = GetCurrentUserContext();
        var request = new CreateFacilityIssueRequest
        {
            LocationId = locationId,
            EquipmentId = equipmentId,
            Title = title,
            Description = description,
            Severity = severity
        };

        var result = await _facilityService.CreateFacilityIssueAsync(request, userId, image, cancellationToken);
        return CreatedAtAction(nameof(GetFacilityIssueById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing facility issue description, severity, or location.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(FacilityIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFacilityIssue(
        Guid id,
        [FromBody] UpdateFacilityIssueRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.UpdateFacilityIssueAsync(id, request, userId, roles, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Transition the status of a facility issue following the state machine.
    /// Staff/Admin required for protected states. Resolution notes required for RESOLVED status.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(typeof(FacilityIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionStatus(
        Guid id,
        [FromBody] IssueStatusTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.TransitionIssueStatusAsync(id, request, userId, roles, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Upload and attach an image to an existing facility issue.
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IssueImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.UploadIssueImageAsync(id, file, userId, roles, cancellationToken);
        return CreatedAtAction(nameof(GetFacilityIssueById), new { id }, result);
    }

    /// <summary>
    /// Retrieve the audit trail and status transition history of a facility issue.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<IssueHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssueHistory(Guid id, CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.GetIssueHistoryAsync(id, userId, roles, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve the AI workflow execution status for a facility issue.
    /// Used by Flutter mobile app to observe automated diagnosis, repair order, and repair scheduling status.
    /// </summary>
    [HttpGet("{id:guid}/workflow")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssueWorkflow(
        Guid id,
        [FromServices] SmartGym.Api.Data.SmartGymDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();

        var issue = await dbContext.FacilityIssues
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (issue == null)
            return NotFound(new { message = "Facility issue not found" });

        var isStaffOrAdmin = roles.Contains(AppRoles.Admin) || roles.Contains("ADMIN") ||
                             roles.Contains(AppRoles.Trainer) || roles.Contains("TRAINER") ||
                             roles.Contains("Staff") || roles.Contains("STAFF") ||
                             roles.Contains("FacilityManager") || roles.Contains("FACILITY_MANAGER");

        var member = await dbContext.Members.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);
        if (!isStaffOrAdmin && (member == null || issue.ReportedByMemberId != member.Id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden: You are not authorized to view this issue's workflow." });
        }

        var workflow = await dbContext.AIWorkflows
            .Include(w => w.FacilityIssue)
            .AsNoTracking()
            .OrderByDescending(w => w.StartedAt)
            .FirstOrDefaultAsync(w => w.IssueId == id, cancellationToken);

        if (workflow == null)
        {
            return NotFound(new { message = "No AI workflow found for this facility issue." });
        }

        return Ok(new
        {
            workflowId = workflow.Id,
            issueId = workflow.IssueId,
            status = workflow.Status.ToString(),
            currentStep = workflow.CurrentStep,
            diagnosisSummary = workflow.DiagnosisSummary,
            recommendedAction = workflow.RecommendedAction,
            estimatedConfidenceScore = workflow.EstimatedConfidenceScore,
            requiresHumanApproval = workflow.RequiresHumanApproval,
            humanApprovalGranted = workflow.HumanApprovalGranted,
            facilityIssueStatus = workflow.FacilityIssue?.Status.ToString() ?? issue.Status.ToString(),
            startedAt = workflow.StartedAt,
            completedAt = workflow.CompletedAt
        });
    }
}
