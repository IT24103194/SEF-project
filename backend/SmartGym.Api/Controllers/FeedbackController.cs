using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IFacilityService facilityService, ILogger<FeedbackController> logger)
    {
        _facilityService = facilityService;
        _logger = logger;
    }

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
    /// Retrieve paginated feedback records. Members see only their own; Staff/Admin see all.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<FeedbackDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeedbacks([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.GetFeedbacksAsync(request, userId, roles, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve feedback details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedbackById(Guid id, CancellationToken cancellationToken)
    {
        var (userId, roles) = GetCurrentUserContext();
        var result = await _facilityService.GetFeedbackByIdAsync(id, userId, roles, cancellationToken);
        return Ok(result);
    }
    //commit

    /// <summary>
    /// Submit feedback with rating and optional offensive-word content moderation.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var (userId, _) = GetCurrentUserContext();
        var result = await _facilityService.CreateFeedbackAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetFeedbackById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Staff/Admin responds to member feedback.
    /// </summary>
    [HttpPost("{id:guid}/respond")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondFeedback(
        Guid id,
        [FromBody] RespondFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = GetCurrentUserContext();
        var result = await _facilityService.RespondFeedbackAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete feedback record (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeedback(Guid id, CancellationToken cancellationToken)
    {
        await _facilityService.DeleteFeedbackAsync(id, cancellationToken);
        return NoContent();
    }
}
