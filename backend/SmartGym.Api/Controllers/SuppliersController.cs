using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Inventory;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SuppliersController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(IInventoryService inventoryService, ILogger<SuppliersController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve paginated, searchable, and sortable suppliers.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetSuppliersAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve supplier details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplier(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetSupplierByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new supplier (Staff / Trainer / Admin required).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.CreateSupplierAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSupplier), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update existing supplier details.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.UpdateSupplierAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a supplier if no associated products or orders exist (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSupplier(Guid id, CancellationToken cancellationToken)
    {
        await _inventoryService.DeleteSupplierAsync(id, cancellationToken);
        return NoContent();
    }
}
