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
[Route("api/bookings")]
[Produces("application/json")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IClassManagementService _service;
    private readonly SmartGym.Api.Data.SmartGymDbContext _context;

    public BookingsController(IClassManagementService service, SmartGym.Api.Data.SmartGymDbContext context)
    {
        _service = service;
        _context = context;
    }

    private (Guid UserId, bool IsStaff) GetUserContext()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Trainer") || User.IsInRole("ADMIN") || User.IsInRole("TRAINER");
        return (userId, isStaff);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? scheduleId,
        [FromQuery] Guid? memberId,
        [FromQuery] BookingStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();

        // If member is not staff, scope to their own member record
        if (!isStaff)
        {
            var member = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(_context.Members, m => m.UserId == userId, cancellationToken);

            if (member == null)
            {
                return Ok(PagedResult<BookingDto>.Create(Array.Empty<BookingDto>(), 0, page, pageSize));
            }
            memberId = member.Id;
        }

        var result = await _service.GetBookingsAsync(scheduleId, memberId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-bookings")]
    [ProducesResponseType(typeof(List<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] bool? upcomingOnly,
        CancellationToken cancellationToken)
    {
        var (userId, _) = GetUserContext();
        var member = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(_context.Members, m => m.UserId == userId, cancellationToken);

        if (member == null)
            return Ok(new List<BookingDto>());

        var result = await _service.GetMemberBookingsAsync(member.Id, upcomingOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var (userId, isStaff) = GetUserContext();
        var result = await _service.GetBookingByIdAsync(id, userId, isStaff, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookClass([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var (userId, isStaff) = GetUserContext();
        var result = await _service.BookClassAsync(request, userId, isStaff, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, isStaff) = GetUserContext();
        var result = await _service.CancelBookingAsync(id, request, userId, isStaff, cancellationToken);
        return Ok(result);
    }
}
