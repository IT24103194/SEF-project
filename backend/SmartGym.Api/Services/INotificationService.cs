using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Notifications;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface INotificationService
{
    // Core CRUD & retrieval
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool? unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationDto> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    // Business Event Notifications
    Task<NotificationDto> NotifyBookingConfirmationAsync(Guid userId, string className, DateTime startTime, string room, CancellationToken cancellationToken = default);
    Task<NotificationDto> NotifyBookingCancellationAsync(Guid userId, string className, DateTime startTime, string reason, CancellationToken cancellationToken = default);
    Task<NotificationDto> NotifyMembershipExpiryAsync(Guid userId, string planName, DateTime expiryDate, int daysRemaining, CancellationToken cancellationToken = default);
    Task<NotificationDto> NotifyIssueUpdateAsync(Guid userId, Guid issueId, string title, string oldStatus, string newStatus, CancellationToken cancellationToken = default);
    Task<NotificationDto> NotifyRepairApprovalAsync(Guid adminUserId, Guid repairOrderId, string equipmentName, decimal estimatedCost, CancellationToken cancellationToken = default);
    Task<NotificationDto> NotifyRepairScheduledAsync(Guid userId, Guid repairOrderId, string equipmentName, DateTime scheduledDate, CancellationToken cancellationToken = default);
    Task<NotificationDto> NotifyVendorRequestAsync(Guid supplierUserId, Guid purchaseOrderId, string poNumber, decimal totalAmount, CancellationToken cancellationToken = default);
}
