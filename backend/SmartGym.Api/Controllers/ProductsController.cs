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
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IInventoryService inventoryService, ILogger<ProductsController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Search, filter, and paginate products.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] PagedRequest request,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? supplierId,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetProductsAsync(request, categoryId, supplierId, activeOnly, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve product details including current inventory stock by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetProductByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new product and initialize its inventory item (Staff / Trainer / Admin).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _inventoryService.CreateProductAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update existing product information.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a product (Admin only). Fails if stock movements or order history exist.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        await _inventoryService.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieve all product categories with product counts.
    /// </summary>
    [HttpGet("categories")]
    [Authorize]
    [ProducesResponseType(typeof(List<ProductCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new product category.
    /// </summary>
    [HttpPost("categories")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.CreateCategoryAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}
