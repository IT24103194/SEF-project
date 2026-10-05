using System.ComponentModel.DataAnnotations;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Validators;

/// <summary>
/// Domain validator enforcing SLA, priority, and escalation business rules
/// for SmartGym facility issues (Component 2 - Student 2 IT24103362).
/// </summary>
public static class FacilityIssueValidator
{
    public static List<ValidationResult> ValidateIssueCreation(CreateFacilityIssueRequest request)
    {
        var results = new List<ValidationResult>();

        if (request.Severity == IssueSeverity.Critical && request.Urgency == IssueUrgencyLevel.Normal)
        {
            results.Add(new ValidationResult(
                "Critical severity issues cannot have Normal urgency. Please escalate to Elevated or CriticalEmergency.",
                new[] { nameof(request.Urgency) }));
        }

        if (request.Urgency == IssueUrgencyLevel.CriticalEmergency && string.IsNullOrWhiteSpace(request.EmergencyEscalationContact))
        {
            results.Add(new ValidationResult(
                "Emergency escalation contact is required when reporting a CriticalEmergency issue.",
                new[] { nameof(request.EmergencyEscalationContact) }));
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 5)
        {
            results.Add(new ValidationResult(
                "Title must be at least 5 characters in length.",
                new[] { nameof(request.Title) }));
        }

        return results;
    }

    public static TimeSpan GetTargetResolutionSla(IssueSeverity severity, IssueUrgencyLevel urgency)
    {
        // SLA response targets based on severity and urgency
        return (severity, urgency) switch
        {
            (IssueSeverity.Critical, _) => TimeSpan.FromHours(4),
            (_, IssueUrgencyLevel.CriticalEmergency) => TimeSpan.FromHours(6),
            (IssueSeverity.High, IssueUrgencyLevel.Elevated) => TimeSpan.FromHours(12),
            (IssueSeverity.High, _) => TimeSpan.FromHours(24),
            (IssueSeverity.Medium, IssueUrgencyLevel.Elevated) => TimeSpan.FromHours(24),
            (IssueSeverity.Medium, _) => TimeSpan.FromHours(48),
            _ => TimeSpan.FromHours(72)
        };
    }
}
