using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/repair-orders")]
[Produces("application/json")]
public class RepairOrdersController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly ILogger<RepairOrdersController> _logger;

    public RepairOrdersController(IFacilityService facilityService, ILogger<RepairOrdersController> logger)
    {
        _facilityService = facilityService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user identity in token.");
        }
        return userId;
    }

    /// <summary>
    /// Retrieve paginated repair orders filtered by issue, equipment, or status.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(PagedResult<RepairOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairOrders(
        [FromQuery] PagedRequest request,
        [FromQuery] Guid? issueId,
        [FromQuery] Guid? equipmentId,
        [FromQuery] RepairOrderStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetRepairOrdersAsync(request, issueId, equipmentId, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve repair order details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRepairOrderById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetRepairOrderByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new repair order. High-value repairs (>= $500 / Rs. 25,000) are automatically held for manager approval.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRepairOrder([FromBody] CreateRepairOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _facilityService.CreateRepairOrderAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetRepairOrderById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update repair order details, actual costs, or status.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRepairOrder(Guid id, [FromBody] UpdateRepairOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _facilityService.UpdateRepairOrderAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a repair order (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRepairOrder(Guid id, CancellationToken cancellationToken)
    {
        await _facilityService.DeleteRepairOrderAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Human-in-the-Loop financial approval for high-value repairs (Admin only).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(RepairOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRepairApproval(
        Guid id,
        [FromBody] ProcessApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _facilityService.ProcessRepairApprovalAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }
}
