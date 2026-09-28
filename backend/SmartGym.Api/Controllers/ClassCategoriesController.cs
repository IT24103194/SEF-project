using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/class-categories")]
[Produces("application/json")]
public class ClassCategoriesController : ControllerBase
{
    private readonly IClassManagementService _service;

    public ClassCategoriesController(IClassManagementService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ClassCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetCategoryByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ClassCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClassCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(ClassCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }
}
