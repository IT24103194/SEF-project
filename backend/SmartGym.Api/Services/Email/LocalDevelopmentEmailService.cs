using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartGym.Api.Services.Email;

public class LocalDevelopmentEmailService : IEmailService
{
    private readonly ILogger<LocalDevelopmentEmailService>? _logger;
    private static readonly ConcurrentDictionary<string, DateTime> _dispatchedKeys = new();
    private static readonly List<EmailMessage> _sentMessages = new();

    public LocalDevelopmentEmailService(ILogger<LocalDevelopmentEmailService>? logger = null)
    {
        _logger = logger;
    }

    public IReadOnlyList<EmailMessage> GetSentMessages() => _sentMessages.AsReadOnly();

    public Task<EmailSendResult> SendSupplierRepairRequestAsync(
        SupplierRepairEmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Strict Security Rule: Do NOT send before approval
        if (!string.Equals(request.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogWarning("LocalEmailService rejected dispatch: Approval status is '{Status}'.", request.ApprovalStatus);
            throw new InvalidOperationException($"Security Violation: Transactional email cannot be sent when approval status is '{request.ApprovalStatus}'. Mandatory human authorization is required.");
        }

        // 2. Duplicate Request / Idempotency Check
        var idempotencyKey = request.IdempotencyKey ?? $"supplier-repair-{request.RepairOrderNumber}";
        if (_dispatchedKeys.ContainsKey(idempotencyKey))
        {
            _logger?.LogWarning("LocalEmailService: Duplicate vendor email prevented for key '{Key}'", idempotencyKey);
            return Task.FromResult(new EmailSendResult
            {
                IsSuccess = true,
                IsDuplicate = true,
                Provider = "LocalDevelopmentEmailService",
                MessageId = $"DUP-{idempotencyKey}",
                Error = "Duplicate request prevented: Email already dispatched for this repair order."
            });
        }

        // 3. Privacy Rule: Do NOT include unnecessary member personal information
        var sanitizedDescription = SanitizeDescriptionForPrivacy(request.IssueDescription);

        var messageId = $"DEV-MSG-{Guid.NewGuid():N}".Substring(0, 16);
        _dispatchedKeys.TryAdd(idempotencyKey, DateTime.UtcNow);

        var msg = new EmailMessage
        {
            To = request.SupplierEmail,
            Subject = $"Repair Request: {request.EquipmentName} ({request.RepairOrderNumber})",
            BodyText = sanitizedDescription,
            IdempotencyKey = idempotencyKey
        };
        _sentMessages.Add(msg);

        _logger?.LogInformation(
            "LocalDevelopmentEmailService: Dispatched supplier email to '{To}' for RepairOrder '{RO}' (Equipment: {Eq}, Cost: LKR {Cost:N2}). MessageId: {MsgId}",
            request.SupplierEmail,
            request.RepairOrderNumber,
            request.EquipmentName,
            request.EstimatedCost,
            messageId);

        return Task.FromResult(new EmailSendResult
        {
            IsSuccess = true,
            MessageId = messageId,
            Provider = "LocalDevelopmentEmailService",
            AttemptCount = 1,
            SentAt = DateTime.UtcNow
        });
    }

    public Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey) && _dispatchedKeys.ContainsKey(message.IdempotencyKey))
        {
            return Task.FromResult(new EmailSendResult
            {
                IsSuccess = true,
                IsDuplicate = true,
                Provider = "LocalDevelopmentEmailService",
                MessageId = $"DUP-{message.IdempotencyKey}",
                Error = "Duplicate email request prevented."
            });
        }

        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey))
        {
            _dispatchedKeys.TryAdd(message.IdempotencyKey, DateTime.UtcNow);
        }

        var messageId = $"DEV-MSG-{Guid.NewGuid():N}".Substring(0, 16);
        _logger.LogInformation("LocalDevelopmentEmailService: Sent email to '{To}' subject '{Sub}'. MessageId: {MsgId}", message.To, message.Subject, messageId);

        return Task.FromResult(new EmailSendResult
        {
            IsSuccess = true,
            MessageId = messageId,
            Provider = "LocalDevelopmentEmailService",
            AttemptCount = 1,
            SentAt = DateTime.UtcNow
        });
    }

    private static string SanitizeDescriptionForPrivacy(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var emailRegex = new Regex(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);
        var sanitized = emailRegex.Replace(text, "[REDACTED EMAIL]");

        var phoneRegex = new Regex(@"(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}", RegexOptions.Compiled);
        sanitized = phoneRegex.Replace(sanitized, "[REDACTED PHONE]");

        var memberIdRegex = new Regex(@"(?i)member\s*id[:\s]*[a-z0-9\-]+", RegexOptions.Compiled);
        sanitized = memberIdRegex.Replace(sanitized, "[REDACTED MEMBER ID]");

        return sanitized;
    }
}
