using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartGym.Api.Services.Email;

public class ExternalEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalEmailService> _logger;
    private static readonly ConcurrentDictionary<string, DateTime> _dispatchedKeys = new();

    private const int MaxRetries = 3;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public ExternalEmailService(HttpClient httpClient, ILogger<ExternalEmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendSupplierRepairRequestAsync(
        SupplierRepairEmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Mandatory Security Rule: Do NOT send before approval
        if (!string.Equals(request.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rejected email dispatch: Approval status is '{Status}'. High-impact email requires confirmed approval.", request.ApprovalStatus);
            throw new InvalidOperationException($"Security Violation: Transactional email cannot be sent when approval status is '{request.ApprovalStatus}'. Mandatory human authorization is required.");
        }

        // 2. Duplicate Request / Idempotency Check
        var idempotencyKey = request.IdempotencyKey ?? $"supplier-repair-{request.RepairOrderNumber}";
        if (_dispatchedKeys.ContainsKey(idempotencyKey))
        {
            _logger.LogWarning("Duplicate vendor email dispatch prevented for key '{Key}'", idempotencyKey);
            return new EmailSendResult
            {
                IsSuccess = true,
                IsDuplicate = true,
                Provider = "ExternalEmailService",
                MessageId = $"DUP-{idempotencyKey}",
                Error = "Duplicate request prevented: Email already dispatched for this repair order."
            };
        }

        // 3. Privacy Rule: Do NOT include unnecessary member personal information
        var sanitizedDescription = SanitizeDescriptionForPrivacy(request.IssueDescription);

        var subject = $"[SmartGym Authorized Maintenance] Repair Order #{request.RepairOrderNumber} - {request.EquipmentName}";
        var bodyHtml = $@"
            <h2>SmartGym Authorized Maintenance Request</h2>
            <p><strong>Repair Order:</strong> {request.RepairOrderNumber}</p>
            <p><strong>Equipment:</strong> {request.EquipmentName} (Serial: {request.SerialNumber ?? "On File"})</p>
            <p><strong>Authorized Target Cost:</strong> LKR {request.EstimatedCost:N2}</p>
            <p><strong>Issue Diagnosis:</strong></p>
            <blockquote>{sanitizedDescription}</blockquote>
            <p>Please reply with component availability and scheduled technician dispatch timeline.</p>
            <p><em>SmartGym Facility Logistics</em></p>";

        var message = new EmailMessage
        {
            To = request.SupplierEmail,
            Subject = subject,
            BodyHtml = bodyHtml,
            BodyText = $"Repair Order #{request.RepairOrderNumber} for {request.EquipmentName}. Authorized budget: LKR {request.EstimatedCost:N2}. Issue: {sanitizedDescription}",
            IdempotencyKey = idempotencyKey
        };

        var result = await SendWithResilienceAsync(message, cancellationToken);
        if (result.IsSuccess)
        {
            _dispatchedKeys.TryAdd(idempotencyKey, DateTime.UtcNow);
        }

        return result;
    }

    public async Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey) && _dispatchedKeys.ContainsKey(message.IdempotencyKey))
        {
            return new EmailSendResult
            {
                IsSuccess = true,
                IsDuplicate = true,
                Provider = "ExternalEmailService",
                MessageId = $"DUP-{message.IdempotencyKey}",
                Error = "Duplicate email request prevented by idempotency key."
            };
        }

        var result = await SendWithResilienceAsync(message, cancellationToken);
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(message.IdempotencyKey))
        {
            _dispatchedKeys.TryAdd(message.IdempotencyKey, DateTime.UtcNow);
        }

        return result;
    }

    private async Task<EmailSendResult> SendWithResilienceAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            attempt++;
            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                // In production, posts to transactional email API provider (e.g. SendGrid, Postmark, AWS SES)
                var payload = new
                {
                    to = message.To,
                    subject = message.Subject,
                    html = message.BodyHtml,
                    text = message.BodyText,
                    idempotency_key = message.IdempotencyKey
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                // Execute HTTP request
                var response = await _httpClient.PostAsync("/v1/send", content, linkedCts.Token);

                // Rate limiting handling (429 Too Many Requests)
                if (response.StatusCode == (HttpStatusCode)429)
                {
                    _logger.LogWarning("Email provider returned HTTP 429 (Rate Limit). Attempt {Attempt}/{MaxRetries}", attempt, MaxRetries);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(100 * attempt, cancellationToken);
                        continue;
                    }
                    return new EmailSendResult
                    {
                        IsSuccess = false,
                        Provider = "ExternalEmailService",
                        Error = "Email provider rate limit exceeded (HTTP 429).",
                        AttemptCount = attempt
                    };
                }

                // Provider failure handling (5xx)
                if ((int)response.StatusCode >= 500)
                {
                    _logger.LogWarning("Email provider returned server error HTTP {StatusCode}. Attempt {Attempt}/{MaxRetries}", response.StatusCode, attempt, MaxRetries);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(150 * attempt, cancellationToken);
                        continue;
                    }
                    return new EmailSendResult
                    {
                        IsSuccess = false,
                        Provider = "ExternalEmailService",
                        Error = $"Email provider failure: HTTP {(int)response.StatusCode}",
                        AttemptCount = attempt
                    };
                }

                // Parse response
                var respBody = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(respBody) ? "{}" : respBody);
                    var msgId = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : $"MSG-{Guid.NewGuid():N}".Substring(0, 16);

                    return new EmailSendResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        MessageId = msgId,
                        Provider = "ExternalEmailService",
                        AttemptCount = attempt,
                        Error = response.IsSuccessStatusCode ? null : $"Provider returned error: {respBody}"
                    };
                }
                catch (JsonException jEx)
                {
                    _logger.LogWarning(jEx, "Invalid JSON response from email provider: {Body}", respBody);
                    return new EmailSendResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        MessageId = $"MSG-{Guid.NewGuid():N}".Substring(0, 16),
                        Provider = "ExternalEmailService",
                        AttemptCount = attempt,
                        Error = response.IsSuccessStatusCode ? null : "Invalid response format from email provider."
                    };
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException($"Email provider timed out after {DefaultTimeout.TotalSeconds} seconds on attempt {attempt}.");
                _logger.LogWarning(lastException, "Email dispatch timeout on attempt {Attempt}", attempt);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(100 * attempt, cancellationToken);
                    continue;
                }
            }
            catch (HttpRequestException netEx)
            {
                lastException = netEx;
                _logger.LogWarning(netEx, "Network failure during email dispatch on attempt {Attempt}", attempt);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(100 * attempt, cancellationToken);
                    continue;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "Unexpected failure during email dispatch");
                break;
            }
        }

        return new EmailSendResult
        {
            IsSuccess = false,
            Provider = "ExternalEmailService",
            Error = lastException?.Message ?? "Email dispatch failed after maximum retry attempts.",
            AttemptCount = attempt
        };
    }

    /// <summary>
    /// Strips telephone numbers, email addresses, and member identifiers from issue descriptions
    /// sent to external vendors to preserve member privacy.
    /// </summary>
    private static string SanitizeDescriptionForPrivacy(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Mask email addresses
        var emailRegex = new Regex(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);
        var sanitized = emailRegex.Replace(text, "[REDACTED EMAIL]");

        // Mask phone numbers (standard formats)
        var phoneRegex = new Regex(@"(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}", RegexOptions.Compiled);
        sanitized = phoneRegex.Replace(sanitized, "[REDACTED PHONE]");

        // Mask member id patterns
        var memberIdRegex = new Regex(@"(?i)member\s*id[:\s]*[a-z0-9\-]+", RegexOptions.Compiled);
        sanitized = memberIdRegex.Replace(sanitized, "[REDACTED MEMBER ID]");

        return sanitized;
    }
}
