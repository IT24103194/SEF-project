using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/security")]
[Produces("application/json")]
public class SecurityDemoController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult PublicAccess()
    {
        return Ok(new
        {
            message = "Public access granted. No authentication required.",
            status = "Open"
        });
    }

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult AuthenticatedAccess()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        return Ok(new
        {
            message = "Access granted to authenticated user.",
            userId,
            email,
            roles
        });
    }

    [HttpGet("member")]
    [Authorize(Policy = AppPolicies.RequireMember)]
    public IActionResult MemberAccess()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new
        {
            message = "Access granted to Member role.",
            email,
            scope = "MemberZone"
        });
    }

    [HttpGet("trainer")]
    [Authorize(Policy = AppPolicies.RequireTrainer)]
    public IActionResult TrainerAccess()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new
        {
            message = "Access granted to Trainer or Admin role.",
            email,
            scope = "TrainerZone"
        });
    }

    [HttpGet("admin")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public IActionResult AdminAccess()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new
        {
            message = "Access granted to Admin role only.",
            email,
            scope = "AdminControlCenter"
        });
    }
}
