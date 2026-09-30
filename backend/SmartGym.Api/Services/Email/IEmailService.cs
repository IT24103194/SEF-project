namespace SmartGym.Api.Services.Email;

/// <summary>
/// Transactional email abstraction for SmartGym business and vendor dispatches.
/// Strictly enforces human approval checks, member privacy protection, and idempotency.
/// </summary>
public interface IEmailService
{
    Task<EmailSendResult> SendSupplierRepairRequestAsync(SupplierRepairEmailRequest request, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
