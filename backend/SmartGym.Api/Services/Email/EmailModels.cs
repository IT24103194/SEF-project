using System.Text.Json.Serialization;

namespace SmartGym.Api.Services.Email;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class SupplierRepairEmailRequest
{
    public string SupplierEmail { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string RepairOrderNumber { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty; // Must be "APPROVED"
    public string? IdempotencyKey { get; set; }
    public Guid WorkflowId { get; set; }
}

public class EmailSendResult
{
    public bool IsSuccess { get; set; }
    public string? MessageId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Error { get; set; }
    public bool IsDuplicate { get; set; }
    public int AttemptCount { get; set; } = 1;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
