using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EquipmentController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly ILogger<EquipmentController> _logger;

    public EquipmentController(IFacilityService facilityService, ILogger<EquipmentController> logger)
    {
        _facilityService = facilityService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve paginated gym equipment filtered by location, status, or search keywords.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<EquipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEquipment(
        [FromQuery] PagedRequest request,
        [FromQuery] Guid? locationId,
        [FromQuery] EquipmentStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetEquipmentAsync(request, locationId, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve equipment details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEquipmentById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetEquipmentByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new equipment asset (Staff / Admin required).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEquipment([FromBody] CreateEquipmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.CreateEquipmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEquipmentById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update equipment details, operational status, or location (Staff / Admin required).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEquipment(Guid id, [FromBody] UpdateEquipmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.UpdateEquipmentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete equipment asset (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEquipment(Guid id, CancellationToken cancellationToken)
    {
        await _facilityService.DeleteEquipmentAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieve full maintenance history, past repair orders, and issue logs for a specific equipment asset.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [Authorize]
    [ProducesResponseType(typeof(EquipmentHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEquipmentHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetEquipmentHistoryAsync(id, cancellationToken);
        return Ok(result);
    }
}
