import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/notification_model.dart';
import '../services/notification_api_service.dart';

class NotificationState {
  final bool isLoading;
  final List<NotificationModel> notifications;
  final int unreadCount;
  final NotificationSummaryModel? summary;
  final bool unreadOnlyFilter;
  final String? errorMessage;

  const NotificationState({
    this.isLoading = false,
    this.notifications = const [],
    this.unreadCount = 0,
    this.summary,
    this.unreadOnlyFilter = false,
    this.errorMessage,
  });

  NotificationState copyWith({
    bool? isLoading,
    List<NotificationModel>? notifications,
    int? unreadCount,
    NotificationSummaryModel? summary,
    bool? unreadOnlyFilter,
    String? errorMessage,
  }) {
    return NotificationState(
      isLoading: isLoading ?? this.isLoading,
      notifications: notifications ?? this.notifications,
      unreadCount: unreadCount ?? this.unreadCount,
      summary: summary ?? this.summary,
      unreadOnlyFilter: unreadOnlyFilter ?? this.unreadOnlyFilter,
      errorMessage: errorMessage,
    );
  }
}

class NotificationNotifier extends StateNotifier<NotificationState> {
  final NotificationApiService _service;

  NotificationNotifier(this._service) : super(const NotificationState());

  Future<void> loadNotifications({bool? unreadOnly}) async {
    final filter = unreadOnly ?? state.unreadOnlyFilter;
    state = state.copyWith(isLoading: true, errorMessage: null, unreadOnlyFilter: filter);

    try {
      final items = await _service.getNotifications(
        unreadOnly: filter ? true : null,
      );
      final unreadCount = await _service.getUnreadCount();
      final summary = await _service.getSummary().catchError((_) => NotificationSummaryModel(
            totalCount: items.length,
            unreadCount: unreadCount,
            criticalCount: 0,
            unreadBookingEvents: 0,
            unreadMembershipEvents: 0,
            unreadFacilityEvents: 0,
          ));

      state = state.copyWith(
        isLoading: false,
        notifications: items,
        unreadCount: unreadCount,
        summary: summary,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
    }
  }

  void setFilter(bool unreadOnly) {
    if (state.unreadOnlyFilter != unreadOnly) {
      loadNotifications(unreadOnly: unreadOnly);
    }
  }

  Future<void> refreshUnreadCount() async {
    try {
      final count = await _service.getUnreadCount();
      state = state.copyWith(unreadCount: count);
    } catch (_) {
      // Non-fatal background refresh failure
    }
  }

  Future<void> markAsRead(String id) async {
    try {
      // Optimistic update
      final updatedList = state.notifications.map<NotificationModel>((n) {
        if (n.id == id) {
          return n.copyWith(isRead: true, readAt: DateTime.now());
        }
        return n;
      }).toList();

      final newCount = (state.unreadCount - 1).clamp(0, 9999);
      state = state.copyWith(notifications: updatedList, unreadCount: newCount);

      await _service.markAsRead(id);
    } catch (e) {
      // Refresh on error to restore consistent state
      await loadNotifications();
      state = state.copyWith(
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
    }
  }

  Future<void> markAllAsRead() async {
    try {
      // Optimistic update
      final now = DateTime.now();
      final updatedList = state.notifications.map<NotificationModel>((n) {
        return n.copyWith(isRead: true, readAt: now);
      }).toList();

      state = state.copyWith(notifications: updatedList, unreadCount: 0);

      await _service.markAllAsRead();
    } catch (e) {
      await loadNotifications();
      state = state.copyWith(
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
    }
  }
}

final notificationProvider =
    StateNotifierProvider<NotificationNotifier, NotificationState>((ref) {
  final service = ref.watch(notificationApiServiceProvider);
  return NotificationNotifier(service);
});
