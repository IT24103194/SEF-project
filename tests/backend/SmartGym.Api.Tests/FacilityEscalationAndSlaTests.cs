using System.ComponentModel.DataAnnotations;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;
using SmartGym.Api.Validators;
using Xunit;

namespace SmartGym.Api.Tests;

public class FacilityEscalationAndSlaTests
{
    [Fact]
    public void ValidateIssueCreation_CriticalSeverityWithNormalUrgency_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateFacilityIssueRequest
        {
            LocationId = Guid.NewGuid(),
            Title = "Broken Cable Cross Machine",
            Description = "The main overhead cable snapped during heavy use.",
            Severity = IssueSeverity.Critical,
            Urgency = IssueUrgencyLevel.Normal
        };

        // Act
        var errors = FacilityIssueValidator.ValidateIssueCreation(request);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("cannot have Normal urgency"));
    }

    [Fact]
    public void ValidateIssueCreation_CriticalEmergencyWithoutContact_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateFacilityIssueRequest
        {
            LocationId = Guid.NewGuid(),
            Title = "Dangerous Weight Stack Jammed",
            Description = "Weight pin jammed at top position and risks falling on members.",
            Severity = IssueSeverity.Critical,
            Urgency = IssueUrgencyLevel.CriticalEmergency,
            EmergencyEscalationContact = null
        };

        // Act
        var errors = FacilityIssueValidator.ValidateIssueCreation(request);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("Emergency escalation contact is required"));
    }

    [Fact]
    public void ValidateIssueCreation_ShortTitle_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateFacilityIssueRequest
        {
            LocationId = Guid.NewGuid(),
            Title = "Bad",
            Description = "Something is wrong.",
            Severity = IssueSeverity.Low,
            Urgency = IssueUrgencyLevel.Normal
        };

        // Act
        var errors = FacilityIssueValidator.ValidateIssueCreation(request);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("at least 5 characters"));
    }

    [Fact]
    public void ValidateIssueCreation_ValidEmergencyRequest_PassesValidation()
    {
        // Arrange
        var request = new CreateFacilityIssueRequest
        {
            LocationId = Guid.NewGuid(),
            Title = "Treadmill Motor Overheating",
            Description = "Motor emitting smoke near power switch.",
            Severity = IssueSeverity.Critical,
            Urgency = IssueUrgencyLevel.CriticalEmergency,
            EmergencyEscalationContact = "facility-manager@smartgym.lk"
        };

        // Act
        var errors = FacilityIssueValidator.ValidateIssueCreation(request);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(IssueSeverity.Critical, IssueUrgencyLevel.CriticalEmergency, 4)]
    [InlineData(IssueSeverity.High, IssueUrgencyLevel.Elevated, 12)]
    [InlineData(IssueSeverity.High, IssueUrgencyLevel.Normal, 24)]
    [InlineData(IssueSeverity.Medium, IssueUrgencyLevel.Elevated, 24)]
    [InlineData(IssueSeverity.Medium, IssueUrgencyLevel.Normal, 48)]
    [InlineData(IssueSeverity.Low, IssueUrgencyLevel.Normal, 72)]
    public void GetTargetResolutionSla_ReturnsExpectedHours(IssueSeverity severity, IssueUrgencyLevel urgency, int expectedHours)
    {
        // Act
        var sla = FacilityIssueValidator.GetTargetResolutionSla(severity, urgency);

        // Assert
        Assert.Equal(TimeSpan.FromHours(expectedHours), sla);
    }

    [Theory]
    [InlineData(5, "Positive")]
    [InlineData(4, "Positive")]
    [InlineData(3, "Neutral")]
    [InlineData(2, "NeedsAttention")]
    [InlineData(1, "NeedsAttention")]
    public void FeedbackSentiment_CategorizesRatingCorrectly(int rating, string expectedSentiment)
    {
        // Arrange
        var feedback = new FeedbackDto
        {
            Id = Guid.NewGuid(),
            Rating = rating,
            Subject = "Test Feedback",
            Content = "Sample evaluation"
        };

        // Act & Assert
        Assert.Equal(expectedSentiment, feedback.Sentiment);
    }

    [Fact]
    public void FacilityIssueDto_IsOverdue_CalculatesCorrectly()
    {
        // Arrange: Issue reported 5 hours ago with a 4-hour SLA
        var reportedAt = DateTime.UtcNow.AddHours(-5);
        var targetResolution = reportedAt.AddHours(4);

        var issue = new FacilityIssueDto
        {
            Id = Guid.NewGuid(),
            Title = "Overdue Issue",
            Status = FacilityIssueStatus.IN_PROGRESS,
            ReportedAt = reportedAt,
            TargetResolutionTime = targetResolution
        };

        // Act & Assert
        Assert.True(issue.IsOverdue);

        // Once resolved, it is no longer marked overdue
        issue.Status = FacilityIssueStatus.Resolved;
        Assert.False(issue.IsOverdue);
    }
}
