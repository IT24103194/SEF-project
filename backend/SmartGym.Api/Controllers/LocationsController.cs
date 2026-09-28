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
public class LocationsController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(IFacilityService facilityService, ILogger<LocationsController> logger)
    {
        _facilityService = facilityService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve paginated gym locations and zones.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetLocationsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve location details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocation(Guid id, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetLocationByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new gym facility location or zone (Staff / Admin required).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.CreateLocationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLocation), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing facility location (Staff / Admin required).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.UpdateLocationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a location (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken cancellationToken)
    {
        await _facilityService.DeleteLocationAsync(id, cancellationToken);
        return NoContent();
    }
}
