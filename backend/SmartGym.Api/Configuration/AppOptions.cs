namespace SmartGym.Api.Configuration;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SmartGymApi";
    public string Audience { get; set; } = "SmartGymClients";
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

public class AiServiceSettings
{
    public const string SectionName = "AiService";
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string ApiKey { get; set; } = string.Empty;
    public decimal ApprovalCostThreshold { get; set; } = 25000.0m;
    public int MaxRetries { get; set; } = 3;
    public int ToolTimeoutSeconds { get; set; } = 10;
}

public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string Provider { get; set; } = "smtp";
    public string From { get; set; } = "notifications@smartgym.local";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
}
