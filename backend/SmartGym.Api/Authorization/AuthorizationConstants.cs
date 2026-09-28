namespace SmartGym.Api.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Trainer = "Trainer";
    public const string Member = "Member";

    public static readonly string[] AllRoles = { Admin, Trainer, Member };

    public static bool IsValidRole(string role)
    {
        return AllRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeRole(string role)
    {
        var matched = AllRoles.FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
        return matched ?? role;
    }
}

public static class AppPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireTrainer = "RequireTrainer";
    public const string RequireMember = "RequireMember";
    public const string RequireStaff = "RequireStaff";
}
