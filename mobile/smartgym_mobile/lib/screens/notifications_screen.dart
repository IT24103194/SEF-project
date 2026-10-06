import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/notification_model.dart';
import '../providers/notification_provider.dart';
import '../widgets/animated_gym_background.dart';

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});

  @override
  ConsumerState<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends ConsumerState<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(notificationProvider.notifier).loadNotifications();
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(notificationProvider);
    final notifier = ref.read(notificationProvider.notifier);

    return AnimatedGymBackground(
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827).withValues(alpha: 0.85),
        elevation: 0,
        title: Row(
          children: [
            const Text(
              'Notifications',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
            ),
            if (state.unreadCount > 0) ...[
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: const Color(0xFFEF4444),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  '${state.unreadCount}',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          ],
        ),
        actions: [
          if (state.unreadCount > 0)
            TextButton.icon(
              onPressed: () => notifier.markAllAsRead(),
              icon: const Icon(Icons.done_all, color: Color(0xFF6366F1), size: 18),
              label: const Text(
                'Mark All Read',
                style: TextStyle(
                  color: Color(0xFF6366F1),
                  fontWeight: FontWeight.w600,
                  fontSize: 13,
                ),
              ),
            ),
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: () => notifier.loadNotifications(),
          ),
        ],
      ),
      child: RefreshIndicator(
        color: const Color(0xFF6366F1),
        backgroundColor: const Color(0xFF1E293B),
        onRefresh: () => notifier.loadNotifications(),
        child: Column(
          children: [
            // Filter Pills & Summary Banner
            Container(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
              color: const Color(0xFF111827),
              child: Column(
                children: [
                  // Filter Chips
                  Row(
                    children: [
                      _FilterChip(
                        label: 'All Updates',
                        isSelected: !state.unreadOnlyFilter,
                        onTap: () => notifier.setFilter(false),
                      ),
                      const SizedBox(width: 8),
                      _FilterChip(
                        label: 'Unread Only (${state.unreadCount})',
                        isSelected: state.unreadOnlyFilter,
                        onTap: () => notifier.setFilter(true),
                        accentColor: state.unreadCount > 0 ? const Color(0xFFEF4444) : null,
                      ),
                    ],
                  ),
                ],
              ),
            ),

            if (state.errorMessage != null)
              Container(
                margin: const EdgeInsets.all(16),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFFEF4444).withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0xFFEF4444)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline, color: Color(0xFFEF4444), size: 20),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        state.errorMessage!,
                        style: const TextStyle(color: Color(0xFFFCA5A5), fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),

            // Content List
            Expanded(
              child: state.isLoading && state.notifications.isEmpty
                  ? const Center(
                      child: CircularProgressIndicator(color: Color(0xFF6366F1)),
                    )
                  : state.notifications.isEmpty
                      ? _EmptyNotificationsView(unreadOnly: state.unreadOnlyFilter)
                      : ListView.separated(
                          padding: const EdgeInsets.all(16),
                          itemCount: state.notifications.length,
                          separatorBuilder: (_, __) => const SizedBox(height: 12),
                          itemBuilder: (context, index) {
                            final notification = state.notifications[index];
                            return _NotificationCard(
                              notification: notification,
                              onMarkRead: () => notifier.markAsRead(notification.id),
                            );
                          },
                        ),
            ),
          ],
        ),
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  final String label;
  final bool isSelected;
  final VoidCallback onTap;
  final Color? accentColor;

  const _FilterChip({
    required this.label,
    required this.isSelected,
    required this.onTap,
    this.accentColor,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected
              ? (accentColor ?? const Color(0xFF6366F1))
              : const Color(0xFF1E293B),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isSelected
                ? (accentColor ?? const Color(0xFF6366F1))
                : const Color(0xFF334155),
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: isSelected ? Colors.white : const Color(0xFF94A3B8),
            fontSize: 12,
            fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
          ),
        ),
      ),
    );
  }
}

class _NotificationCard extends StatelessWidget {
  final NotificationModel notification;
  final VoidCallback onMarkRead;

  const _NotificationCard({
    required this.notification,
    required this.onMarkRead,
  });

  @override
  Widget build(BuildContext context) {
    final eventStyle = _getEventStyle(notification.eventType, notification.category);
    final isUnread = !notification.isRead;

    return Container(
      decoration: BoxDecoration(
        color: isUnread ? const Color(0xFF131D31) : const Color(0xFF111827),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isUnread ? const Color(0xFF3B82F6) : const Color(0xFF1E293B),
          width: isUnread ? 1.5 : 1.0,
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Event Icon
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: eventStyle.color.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Icon(eventStyle.icon, color: eventStyle.color, size: 20),
                ),
                const SizedBox(width: 12),

                // Title and Meta
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              notification.title,
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 14,
                                fontWeight: isUnread ? FontWeight.w700 : FontWeight.w600,
                              ),
                            ),
                          ),
                          if (isUnread) ...[
                            Container(
                              width: 8,
                              height: 8,
                              decoration: const BoxDecoration(
                                color: Color(0xFF3B82F6),
                                shape: BoxShape.circle,
                              ),
                            ),
                            const SizedBox(width: 6),
                          ],
                        ],
                      ),
                      const SizedBox(height: 2),
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                            decoration: BoxDecoration(
                              color: const Color(0xFF1E293B),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              notification.category.toUpperCase(),
                              style: const TextStyle(
                                color: Color(0xFF94A3B8),
                                fontSize: 10,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          _PriorityBadge(priority: notification.priority),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),

            // Message Body
            Text(
              notification.message,
              style: const TextStyle(
                color: Color(0xFFCBD5E1),
                fontSize: 13,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 10),

            // Footer: Time & Action
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  _formatDate(notification.createdAt),
                  style: const TextStyle(
                    color: Color(0xFF64748B),
                    fontSize: 11,
                  ),
                ),
                if (isUnread)
                  InkWell(
                    onTap: onMarkRead,
                    borderRadius: BorderRadius.circular(6),
                    child: const Padding(
                      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      child: Row(
                        children: [
                          Icon(Icons.check, size: 14, color: Color(0xFF6366F1)),
                          SizedBox(width: 4),
                          Text(
                            'Mark as read',
                            style: TextStyle(
                              color: Color(0xFF6366F1),
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  static String _formatDate(DateTime date) {
    try {
      final now = DateTime.now();
      final diff = now.difference(date);

      if (diff.inMinutes < 1) return 'Just now';
      if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
      if (diff.inHours < 24) return '${diff.inHours}h ago';
      if (diff.inDays < 7) return '${diff.inDays}d ago';
      return '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    } catch (_) {
      return date.toString();
    }
  }

  static _EventStyle _getEventStyle(String eventType, String category) {
    switch (eventType) {
      case 'BookingConfirmation':
        return const _EventStyle(Icons.check_circle_outline, Color(0xFF10B981));
      case 'BookingCancellation':
        return const _EventStyle(Icons.cancel_outlined, Color(0xFFF59E0B));
      case 'MembershipExpiry':
        return const _EventStyle(Icons.timer_outlined, Color(0xFFEC4899));
      case 'IssueUpdate':
        return const _EventStyle(Icons.build_circle_outlined, Color(0xFF06B6D4));
      case 'RepairApproval':
        return const _EventStyle(Icons.verified_outlined, Color(0xFF6366F1));
      case 'RepairScheduled':
        return const _EventStyle(Icons.calendar_today_outlined, Color(0xFF3B82F6));
      case 'VendorRequest':
        return const _EventStyle(Icons.storefront_outlined, Color(0xFF8B5CF6));
      default:
        return const _EventStyle(Icons.notifications_outlined, Color(0xFF64748B));
    }
  }
}

class _EventStyle {
  final IconData icon;
  final Color color;
  const _EventStyle(this.icon, this.color);
}

class _PriorityBadge extends StatelessWidget {
  final String priority;

  const _PriorityBadge({required this.priority});

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color text;

    switch (priority.toUpperCase()) {
      case 'CRITICAL':
        bg = const Color(0xFFEF4444).withValues(alpha: 0.2);
        text = const Color(0xFFEF4444);
        break;
      case 'HIGH':
        bg = const Color(0xFFF59E0B).withValues(alpha: 0.2);
        text = const Color(0xFFF59E0B);
        break;
      case 'LOW':
        bg = const Color(0xFF64748B).withValues(alpha: 0.2);
        text = const Color(0xFF94A3B8);
        break;
      default:
        bg = const Color(0xFF3B82F6).withValues(alpha: 0.2);
        text = const Color(0xFF60A5FA);
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        priority.toUpperCase(),
        style: TextStyle(
          color: text,
          fontSize: 10,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _EmptyNotificationsView extends StatelessWidget {
  final bool unreadOnly;

  const _EmptyNotificationsView({required this.unreadOnly});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: const BoxDecoration(
                color: Color(0xFF1E293B),
                shape: BoxShape.circle,
              ),
              child: Icon(
                unreadOnly ? Icons.mark_email_read_outlined : Icons.notifications_none_outlined,
                size: 40,
                color: const Color(0xFF64748B),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              unreadOnly ? 'No Unread Notifications' : 'No Notifications Yet',
              style: const TextStyle(
                color: Colors.white,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              unreadOnly
                  ? 'You are all caught up on your gym alerts and activity updates.'
                  : 'Booking receipts, facility tickets, and membership alerts will appear here.',
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: Color(0xFF64748B),
                fontSize: 13,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
