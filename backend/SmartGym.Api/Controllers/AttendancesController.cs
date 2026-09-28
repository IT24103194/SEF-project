using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/attendances")]
[Route("api/attendance")]
[Produces("application/json")]
[Authorize(Policy = AppPolicies.RequireStaff)]
public class AttendancesController : ControllerBase
{
    private readonly IClassManagementService _service;

    public AttendancesController(IClassManagementService service)
    {
        _service = service;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AttendanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? scheduleId,
        [FromQuery] Guid? memberId,
        [FromQuery] AttendanceStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAttendancesAsync(scheduleId, memberId, status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("schedule/{scheduleId:guid}")]
    [ProducesResponseType(typeof(List<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleSheet(Guid scheduleId, CancellationToken cancellationToken)
    {
        var result = await _service.GetScheduleAttendanceSheetAsync(scheduleId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordAttendance([FromBody] RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _service.RecordAttendanceAsync(request, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(List<AttendanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordBulkAttendance([FromBody] BulkAttendanceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _service.RecordBulkAttendanceAsync(request, userId, cancellationToken);
        return Ok(result);
    }
}
