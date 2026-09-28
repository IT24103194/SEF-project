using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Common.BusinessHelpers;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Notifications;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var result = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new { unreadCount = count });
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(NotificationSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var summary = await _notificationService.GetSummaryAsync(userId, cancellationToken);
        return Ok(summary);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var updated = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        return Ok(updated);
    }

    [HttpPut("read-all")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var count = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { markedCount = count });
    }

    [HttpPost("events/trigger")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerEvent(
        [FromBody] TriggerEventNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetUserId = request.UserId ?? User.GetUserId();

        NotificationDto dto = request.EventType switch
        {
            NotificationEventType.BookingConfirmation =>
                await _notificationService.NotifyBookingConfirmationAsync(
                    targetUserId,
                    request.ReferenceName ?? "Strength & Conditioning",
                    request.ScheduledDate ?? DateTime.UtcNow.AddDays(1),
                    request.Details ?? "Main Studio",
                    cancellationToken),

            NotificationEventType.BookingCancellation =>
                await _notificationService.NotifyBookingCancellationAsync(
                    targetUserId,
                    request.ReferenceName ?? "Strength & Conditioning",
                    request.ScheduledDate ?? DateTime.UtcNow.AddDays(1),
                    request.Details ?? "Trainer unavailable",
                    cancellationToken),

            NotificationEventType.MembershipExpiry =>
                await _notificationService.NotifyMembershipExpiryAsync(
                    targetUserId,
                    request.ReferenceName ?? "Platinum All-Access",
                    request.ScheduledDate ?? DateTime.UtcNow.AddDays(7),
                    7,
                    cancellationToken),

            NotificationEventType.IssueUpdate =>
                await _notificationService.NotifyIssueUpdateAsync(
                    targetUserId,
                    Guid.NewGuid(),
                    request.ReferenceName ?? "Treadmill #4 Display Malfunction",
                    "SUBMITTED",
                    "IN_PROGRESS",
                    cancellationToken),

            NotificationEventType.RepairApproval =>
                await _notificationService.NotifyRepairApprovalAsync(
                    targetUserId,
                    Guid.NewGuid(),
                    request.ReferenceName ?? "LifeFitness Cable Cross Over",
                    request.Amount ?? 1250.00m,
                    cancellationToken),

            NotificationEventType.RepairScheduled =>
                await _notificationService.NotifyRepairScheduledAsync(
                    targetUserId,
                    Guid.NewGuid(),
                    request.ReferenceName ?? "LifeFitness Cable Cross Over",
                    request.ScheduledDate ?? DateTime.UtcNow.AddDays(3),
                    cancellationToken),

            NotificationEventType.VendorRequest =>
                await _notificationService.NotifyVendorRequestAsync(
                    targetUserId,
                    Guid.NewGuid(),
                    request.ReferenceName ?? "PO-2026-0042",
                    request.Amount ?? 4500.00m,
                    cancellationToken),

            _ => await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = targetUserId,
                Title = request.ReferenceName ?? "System Notification",
                Message = request.Details ?? "General notification update"
            }, cancellationToken)
        };

        return Ok(dto);
    }
}
