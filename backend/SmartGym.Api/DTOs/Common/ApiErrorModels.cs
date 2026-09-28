using System.Text.Json.Serialization;

namespace SmartGym.Api.DTOs.Common;

public class ApiErrorResponse
{
    public string Type { get; set; } = "https://tools.ietf.org/html/rfc7807";
    public string Title { get; set; } = "An error occurred";
    public int Status { get; set; } = 400;
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; set; }

    public static ApiErrorResponse Create(int status, string title, string detail, string instance, string? correlationId = null, IDictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponse
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = instance,
            CorrelationId = correlationId,
            Errors = errors
        };
    }
}

public class ValidationErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
}
