using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/members")]
[Produces("application/json")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembersController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    private (Guid UserId, bool IsStaff) GetUserContext()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Trainer") || User.IsInRole("ADMIN") || User.IsInRole("TRAINER");
        return (userId, isStaff);
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(PagedResult<MemberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _membershipService.GetMembersAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var (userId, _) = GetUserContext();
        var member = await _membershipService.GetMemberByUserIdAsync(userId, cancellationToken);
        return Ok(member);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var member = await _membershipService.GetMemberByIdAsync(id, cancellationToken);

        if (!isStaff && member.UserId != userId)
        {
            return Forbid();
        }

        return Ok(member);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMemberProfileRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var member = await _membershipService.GetMemberByIdAsync(id, cancellationToken);

        if (!isStaff && member.UserId != userId)
        {
            return Forbid();
        }

        var updated = await _membershipService.UpdateMemberProfileAsync(id, request, cancellationToken);
        return Ok(updated);
    }
}
