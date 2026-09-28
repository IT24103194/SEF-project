using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/membership-plans")]
[Produces("application/json")]
public class MembershipPlansController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipPlansController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MembershipPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var plans = await _membershipService.GetPlansAsync(includeInactive, cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MembershipPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _membershipService.GetPlanByIdAsync(id, cancellationToken);
        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(MembershipPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMembershipPlanRequest request, CancellationToken cancellationToken = default)
    {
        var created = await _membershipService.CreatePlanAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(MembershipPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMembershipPlanRequest request, CancellationToken cancellationToken = default)
    {
        var updated = await _membershipService.UpdatePlanAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _membershipService.DeletePlanAsync(id, cancellationToken);
        return NoContent();
    }
}
