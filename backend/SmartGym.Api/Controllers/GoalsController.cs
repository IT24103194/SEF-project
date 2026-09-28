using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Produces("application/json")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public GoalsController(IMembershipService membershipService)
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

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? memberId,
        [FromQuery] GoalStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();

        if (!isStaff)
        {
            var myProfile = await _membershipService.GetMemberByUserIdAsync(userId, cancellationToken);
            memberId = myProfile.Id;
        }

        var result = await _membershipService.GetGoalsAsync(memberId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-goals")]
    [ProducesResponseType(typeof(List<GoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGoals(CancellationToken cancellationToken = default)
    {
        var (userId, _) = GetUserContext();
        var goals = await _membershipService.GetMyGoalsAsync(userId, cancellationToken);
        return Ok(goals);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var goal = await _membershipService.GetGoalByIdAsync(id, userId, isStaff, cancellationToken);
        return Ok(goal);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var created = await _membershipService.CreateGoalAsync(request, userId, isStaff, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var updated = await _membershipService.UpdateGoalAsync(id, request, userId, isStaff, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var completed = await _membershipService.CompleteGoalAsync(id, userId, isStaff, cancellationToken);
        return Ok(completed);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        await _membershipService.DeleteGoalAsync(id, userId, isStaff, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/progress")]
    [ProducesResponseType(typeof(List<ProgressRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGoalProgress(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var records = await _membershipService.GetGoalProgressHistoryAsync(id, userId, isStaff, cancellationToken);
        return Ok(records);
    }

    [HttpPost("{id:guid}/progress")]
    [ProducesResponseType(typeof(ProgressRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordProgress(Guid id, [FromBody] CreateProgressRecordRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        request.GoalId = id;
        var created = await _membershipService.RecordProgressAsync(request, userId, isStaff, cancellationToken);
        return CreatedAtAction(nameof(GetGoalProgress), new { id = created.GoalId }, created);
    }
}
