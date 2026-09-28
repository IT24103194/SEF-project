using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/progress-records")]
[Produces("application/json")]
[Authorize]
public class ProgressRecordsController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public ProgressRecordsController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    private (Guid UserId, bool IsStaff) GetUserContext()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Trainer") || User.IsInRole("ADMIN") || User.IsInRole("TRAINER");
        return (userId, isStaff);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProgressRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordProgress([FromBody] CreateProgressRecordRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var record = await _membershipService.RecordProgressAsync(request, userId, isStaff, cancellationToken);
        return CreatedAtAction(nameof(GetByGoalId), new { goalId = record.GoalId }, record);
    }

    [HttpGet("goals/{goalId:guid}")]
    [ProducesResponseType(typeof(List<ProgressRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByGoalId(Guid goalId, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var records = await _membershipService.GetGoalProgressHistoryAsync(goalId, userId, isStaff, cancellationToken);
        return Ok(records);
    }
}
