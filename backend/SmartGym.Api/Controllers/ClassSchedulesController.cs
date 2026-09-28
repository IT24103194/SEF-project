using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/class-schedules")]
[Route("api/schedules")]
[Produces("application/json")]
public class ClassSchedulesController : ControllerBase
{
    private readonly IClassManagementService _service;

    public ClassSchedulesController(IClassManagementService service)
    {
        _service = service;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClassScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? classId,
        [FromQuery] Guid? trainerId,
        [FromQuery] ScheduleStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetSchedulesAsync(startDate, endDate, classId, trainerId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetScheduleByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(ScheduleAvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(
        Guid id,
        [FromQuery] Guid? memberId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetScheduleAvailabilityAsync(id, memberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("trainer/{trainerId:guid}")]
    [ProducesResponseType(typeof(List<ClassScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrainerSchedules(
        Guid trainerId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetTrainerSchedulesAsync(trainerId, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ClassScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClassScheduleRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _service.CreateScheduleAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ClassScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassScheduleRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _service.UpdateScheduleAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ClassScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _service.CancelScheduleAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteScheduleAsync(id, cancellationToken);
        return NoContent();
    }
}
