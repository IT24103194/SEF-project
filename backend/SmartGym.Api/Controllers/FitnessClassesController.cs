using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/fitness-classes")]
[Route("api/classes")]
[Produces("application/json")]
public class FitnessClassesController : ControllerBase
{
    private readonly IClassManagementService _service;

    public FitnessClassesController(IClassManagementService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FitnessClassDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? intensity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetClassesAsync(search, categoryId, intensity, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FitnessClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetClassByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(FitnessClassDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateFitnessClassRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateClassAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(FitnessClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFitnessClassRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateClassAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteClassAsync(id, cancellationToken);
        return NoContent();
    }
}
