using System.ComponentModel.DataAnnotations;

namespace SmartGym.Api.Configuration;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "JWT Signing Key is required.")]
    [MinLength(32, ErrorMessage = "JWT Signing Key must be at least 32 characters (256 bits) long.")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = "SmartGymApi";

    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = "SmartGymClients";

    [Range(1, 1440, ErrorMessage = "JWT ExpiryMinutes must be between 1 and 1440 minutes.")]
    public int ExpiryMinutes { get; set; } = 60;

    [Range(1, 90, ErrorMessage = "RefreshTokenExpiryDays must be between 1 and 90 days.")]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

public class AiServiceSettings
{
    public const string SectionName = "AiService";

    [Required(ErrorMessage = "AI Service BaseUrl is required.")]
    [Url(ErrorMessage = "AI Service BaseUrl must be a valid HTTP/HTTPS URL.")]
    public string BaseUrl { get; set; } = "http://localhost:8000";

    [Required(ErrorMessage = "AI Service ApiKey is required.")]
    public string ApiKey { get; set; } = string.Empty;

    [Range(0, 10000000, ErrorMessage = "ApprovalCostThreshold must be non-negative.")]
    public decimal ApprovalCostThreshold { get; set; } = 25000.0m;

    [Range(1, 10, ErrorMessage = "MaxRetries must be between 1 and 10.")]
    public int MaxRetries { get; set; } = 3;

    [Range(1, 120, ErrorMessage = "ToolTimeoutSeconds must be between 1 and 120 seconds.")]
    public int ToolTimeoutSeconds { get; set; } = 10;
}

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    [Required]
    public string Provider { get; set; } = "smtp";

    [Required]
    [EmailAddress]
    public string From { get; set; } = "notifications@smartgym.local";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 1025;
}
