using System.Text.Json;
using SmartGym.Api.Data;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface IAuditService
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        Guid? userId = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly SmartGymDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(SmartGymDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        Guid? userId = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                OldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit log for entity '{EntityName}' ID '{EntityId}'", entityName, entityId);
            // Non-blocking: audit failure should not break critical path
        }
    }
}
