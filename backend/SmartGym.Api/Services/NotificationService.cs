using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Notifications;
using SmartGym.Api.Entities;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Services;

public class NotificationService : INotificationService
{
    private readonly SmartGymDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(SmartGymDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            TargetUrl = request.TargetUrl?.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification created for User '{UserId}': {Title}", request.UserId, request.Title);

        return MapToDto(notification);
    }

    public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        bool? unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                TargetUrl = n.TargetUrl,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<NotificationDto>.Create(items, total, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        var total = await _context.Notifications
            .CountAsync(n => n.UserId == userId, cancellationToken);

        var recent = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                TargetUrl = n.TargetUrl,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new NotificationSummaryDto
        {
            UnreadCount = unread,
            TotalCount = total,
            RecentNotifications = recent
        };
    }

    public async Task<NotificationDto> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Notification with ID '{notificationId}' was not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(notification);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadList = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var count = unreadList.Count;
        var now = DateTime.UtcNow;
        foreach (var n in unreadList)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    // ==========================================
    // BUSINESS EVENTS
    // ==========================================

    public async Task<NotificationDto> NotifyBookingConfirmationAsync(
        Guid userId,
        string className,
        DateTime startTime,
        string room,
        CancellationToken cancellationToken = default)
    {
        var req = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.Booking,
            Title = "Class Booking Confirmed",
            Message = $"Your spot for '{className}' on {startTime:ddd, MMM d h:mm tt} in {room} is confirmed.",
            TargetUrl = "/classes"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    public async Task<NotificationDto> NotifyBookingCancellationAsync(
        Guid userId,
        string className,
        DateTime startTime,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var req = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.Booking,
            Title = "Class Booking Cancelled",
            Message = $"Your booking for '{className}' scheduled on {startTime:ddd, MMM d h:mm tt} was cancelled. Reason: {reason}",
            TargetUrl = "/classes"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    public async Task<NotificationDto> NotifyMembershipExpiryAsync(
        Guid userId,
        string planName,
        DateTime expiryDate,
        int daysRemaining,
        CancellationToken cancellationToken = default)
    {
        var title = daysRemaining <= 0 ? "Membership Expired" : "Membership Expiring Soon";
        var message = daysRemaining <= 0
            ? $"Your '{planName}' membership has expired on {expiryDate:d}. Renew now to maintain uninterrupted facility access."
            : $"Your '{planName}' membership will expire in {daysRemaining} days on {expiryDate:d}. Renew now to preserve gym privileges.";

        var req = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.General,
            Title = title,
            Message = message,
            TargetUrl = "/membership"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    public async Task<NotificationDto> NotifyIssueUpdateAsync(
        Guid userId,
        Guid issueId,
        string title,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        var req = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.Maintenance,
            Title = "Facility Issue Status Updated",
            Message = $"Facility issue '{title}' status changed from '{oldStatus}' to '{newStatus}'.",
            TargetUrl = $"/facility-issues/{issueId}"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    public async Task<NotificationDto> NotifyRepairApprovalAsync(
        Guid adminUserId,
        Guid repairOrderId,
        string equipmentName,
        decimal estimatedCost,
        CancellationToken cancellationToken = default)
    {
        var req = new CreateNotificationRequest
        {
            UserId = adminUserId,
            Type = NotificationType.Approval,
            Title = "Repair Order Requires Approval",
            Message = $"High-value repair for '{equipmentName}' (Est: ${estimatedCost:N2}) requires administrative approval.",
            TargetUrl = "/facility"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    public async Task<NotificationDto> NotifyRepairScheduledAsync(
        Guid userId,
        Guid repairOrderId,
        string equipmentName,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default)
    {
        var req = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.Maintenance,
            Title = "Equipment Repair Scheduled",
            Message = $"Technician repair for '{equipmentName}' has been scheduled for {scheduledDate:d}.",
            TargetUrl = "/facility"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    public async Task<NotificationDto> NotifyVendorRequestAsync(
        Guid supplierUserId,
        Guid purchaseOrderId,
        string poNumber,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        var req = new CreateNotificationRequest
        {
            UserId = supplierUserId,
            Type = NotificationType.General,
            Title = "New Purchase Order Received",
            Message = $"Purchase Order #{poNumber} totaling ${totalAmount:N2} has been submitted for fulfillment.",
            TargetUrl = "/inventory"
        };
        return await CreateNotificationAsync(req, cancellationToken);
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        TargetUrl = n.TargetUrl,
        CreatedAt = n.CreatedAt
    };
}
