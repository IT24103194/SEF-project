using SmartGym.Api.Entities;

namespace SmartGym.Api.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string TypeName => Type.ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? TargetUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; } = NotificationType.General;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
}

public class NotificationSummaryDto
{
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
}

public class TriggerEventNotificationRequest
{
    public NotificationEventType EventType { get; set; }
    public Guid? UserId { get; set; }
    public string? ReferenceName { get; set; }
    public string? Details { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public enum NotificationEventType
{
    BookingConfirmation = 1,
    BookingCancellation = 2,
    MembershipExpiry = 3,
    IssueUpdate = 4,
    RepairApproval = 5,
    RepairScheduled = 6,
    VendorRequest = 7
}
