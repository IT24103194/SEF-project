using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Entities;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/memberships")]
[Produces("application/json")]
[Authorize]
public class MembershipsController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipsController(IMembershipService membershipService)
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
    [ProducesResponseType(typeof(PagedResult<MembershipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? memberId,
        [FromQuery] MembershipStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();

        // If member is not staff, scope strictly to their own member record
        if (!isStaff)
        {
            var myProfile = await _membershipService.GetMemberByUserIdAsync(userId, cancellationToken);
            memberId = myProfile.Id;
        }

        var result = await _membershipService.GetMembershipsAsync(memberId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-membership")]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMembership(CancellationToken cancellationToken = default)
    {
        var (userId, _) = GetUserContext();
        var membership = await _membershipService.GetMyCurrentMembershipAsync(userId, cancellationToken);
        return Ok(membership);
    }

    [HttpGet("history/{memberId:guid}")]
    [ProducesResponseType(typeof(List<MembershipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHistory(Guid memberId, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();

        if (!isStaff)
        {
            var myProfile = await _membershipService.GetMemberByUserIdAsync(userId, cancellationToken);
            if (myProfile.Id != memberId)
            {
                return Forbid();
            }
        }

        var history = await _membershipService.GetMemberMembershipHistoryAsync(memberId, cancellationToken);
        return Ok(history);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipService.CreateMembershipAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { memberId = membership.MemberId }, membership);
    }

    [HttpPost("renew")]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Renew([FromBody] RenewMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var renewed = await _membershipService.RenewMembershipAsync(request, userId, isStaff, cancellationToken);
        return Ok(renewed);
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var (userId, isStaff) = GetUserContext();
        var cancelled = await _membershipService.CancelMembershipAsync(id, request, userId, isStaff, cancellationToken);
        return Ok(cancelled);
    }

    [HttpGet("analytics")]
    [Authorize(Policy = AppPolicies.RequireStaff)]
    [ProducesResponseType(typeof(MembershipAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken = default)
    {
        var analytics = await _membershipService.GetAnalyticsAsync(cancellationToken);
        return Ok(analytics);
    }
}
