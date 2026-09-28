using System.Security.Claims;
using SmartGym.Api.Authorization;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Common.BusinessHelpers;

public static class AuthorizationHelpers
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }

    public static List<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppRoles.Admin) || principal.IsInRole("ADMIN") || principal.IsInRole("admin");
    }

    public static bool IsTrainer(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppRoles.Trainer) || principal.IsInRole("TRAINER") || principal.IsInRole("trainer");
    }

    public static bool IsStaff(this ClaimsPrincipal principal)
    {
        return principal.IsAdmin() || principal.IsTrainer();
    }

    public static bool IsMember(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppRoles.Member) || principal.IsInRole("MEMBER") || principal.IsInRole("member");
    }

    public static void EnsureCanAccessUserResource(this ClaimsPrincipal principal, Guid targetUserId, string resourceName = "resource")
    {
        var currentUserId = principal.GetUserId();
        if (!principal.IsStaff() && currentUserId != targetUserId)
        {
            throw new ForbiddenException($"You are not authorized to view or manage this {resourceName}.");
        }
    }
}
