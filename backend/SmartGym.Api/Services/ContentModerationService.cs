using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface IContentModerationService
{
    ContentModerationResult ModerateText(string? text);
    Task<AuditLog?> AuditModerationEventAsync(Guid entityId, string entityName, ContentModerationResult result, Guid? userId, CancellationToken cancellationToken = default);
}

public class ContentModerationService : IContentModerationService
{
    private readonly SmartGymDbContext _dbContext;
    private readonly ILogger<ContentModerationService> _logger;

    // Comprehensive deterministic dictionary of inappropriate / offensive / abusive words
    private static readonly HashSet<string> OffensiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "fuck", "fucking", "fucker", "shit", "bullshit", "bitch", "bitches", "asshole",
        "bastard", "crap", "damn", "hell", "idiot", "moron", "scam", "scammer", "fraud",
        "scumbag", "piss", "dick", "cunt", "slut", "whore", "retard", "nigger", "faggot",
        "stupid", "dumbass", "jackass"
    };

    public ContentModerationService(SmartGymDbContext dbContext, ILogger<ContentModerationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public ContentModerationResult ModerateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ContentModerationResult
            {
                IsFlagged = false,
                OriginalText = text ?? string.Empty,
                SanitizedText = text ?? string.Empty,
                ModerationStatus = "Clean",
                ModerationReason = null,
                DetectedKeywords = new List<string>()
            };
        }

        var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sanitized = text;

        foreach (var word in OffensiveKeywords)
        {
            var pattern = $@"\b{Regex.Escape(word)}\b";
            if (Regex.IsMatch(sanitized, pattern, RegexOptions.IgnoreCase))
            {
                detected.Add(word);
                sanitized = Regex.Replace(sanitized, pattern, match =>
                {
                    var len = match.Value.Length;
                    if (len <= 2) return new string('*', len);
                    return match.Value[0] + new string('*', len - 1);
                }, RegexOptions.IgnoreCase);
            }
        }

        var isFlagged = detected.Count > 0;
        return new ContentModerationResult
        {
            IsFlagged = isFlagged,
            OriginalText = text,
            SanitizedText = sanitized,
            ModerationStatus = isFlagged ? "Flagged" : "Clean",
            ModerationReason = isFlagged 
                ? $"Prohibited offensive terms detected: {string.Join(", ", detected)}" 
                : null,
            DetectedKeywords = detected.ToList()
        };
    }

    public async Task<AuditLog?> AuditModerationEventAsync(
        Guid entityId, 
        string entityName, 
        ContentModerationResult result, 
        Guid? userId, 
        CancellationToken cancellationToken = default)
    {
        if (!result.IsFlagged)
        {
            return null;
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId.ToString(),
            Action = "CONTENT_MODERATION",
            UserId = userId,
            OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { rawText = result.OriginalText }),
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                sanitizedText = result.SanitizedText,
                status = result.ModerationStatus,
                reason = result.ModerationReason,
                detectedTerms = result.DetectedKeywords
            }),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning("Content moderation flagged content on {EntityName} ({EntityId}): {Reason}", 
            entityName, entityId, result.ModerationReason);

        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        return auditLog;
    }
}
