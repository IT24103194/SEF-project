using System.Security.Claims;
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
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve paginated inventory items with filtering (low stock, category, supplier) and search.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory([FromQuery] InventoryQueryParameters query, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventoryItemsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve an individual inventory item by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventoryItemByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Adjust stock level with atomic transaction, negative balance prevention, and stock movement auditing.
    /// </summary>
    [HttpPost("{id:guid}/adjust")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _inventoryService.AdjustStockAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger reorder for an inventory item, automatically creating a Purchase Order for its associated supplier.
    /// </summary>
    [HttpPost("{id:guid}/reorder")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ReorderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(Guid id, [FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _inventoryService.ReorderProductAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve chronological stock movement history for an inventory item.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [Authorize]
    [ProducesResponseType(typeof(List<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetStockHistoryAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export current supplement inventory and catalog to standard RFC 4180 CSV format.
    /// </summary>
    [HttpGet("export-csv")]
    [Authorize]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var bytes = await _inventoryService.ExportInventoryCsvAsync(cancellationToken);
        var filename = $"inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", filename);
    }

    /// <summary>
    /// Import inventory catalog items from CSV with row-by-row validation and transaction safety.
    /// </summary>
    [HttpPost("import-csv")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid CSV File",
                Detail = "Please upload a non-empty CSV file."
            });
        }

        var userId = GetCurrentUserId();
        using var stream = file.OpenReadStream();
        var result = await _inventoryService.ImportInventoryCsvAsync(stream, userId, cancellationToken);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}
