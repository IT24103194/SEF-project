import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/notification_model.dart';
import 'package:smartgym_mobile/providers/notification_provider.dart';
import 'package:smartgym_mobile/screens/notifications_screen.dart';

void main() {
  final sampleNotifications = [
    NotificationModel(
      id: 'notif-1',
      userId: 'user-1',
      title: 'Booking Confirmed: HIIT Blast',
      message: 'Your spot in HIIT Blast on Friday 10:00 AM has been reserved.',
      eventType: 'BookingConfirmation',
      category: 'Booking',
      priority: 'Normal',
      isRead: false,
      createdAt: DateTime.now().subtract(const Duration(minutes: 5)),
    ),
    NotificationModel(
      id: 'notif-2',
      userId: 'user-1',
      title: 'Facility Notice: Treadmill Repair',
      message: 'Treadmill #04 in Cardio Zone has been repaired and is operational.',
      eventType: 'IssueUpdate',
      category: 'Facility',
      priority: 'Low',
      isRead: true,
      readAt: DateTime.now().subtract(const Duration(hours: 1)),
      createdAt: DateTime.now().subtract(const Duration(hours: 2)),
    ),
    NotificationModel(
      id: 'notif-3',
      userId: 'user-1',
      title: 'Membership Renewal Warning',
      message: 'Your Platinum membership expires in 3 days. Renew now to maintain access.',
      eventType: 'MembershipExpiry',
      category: 'Membership',
      priority: 'High',
      isRead: false,
      createdAt: DateTime.now().subtract(const Duration(hours: 4)),
    ),
  ];

  testWidgets('NotificationsScreen renders title, badge, and notifications list', (tester) async {
    final state = NotificationState(
      isLoading: false,
      notifications: sampleNotifications,
      unreadCount: 2,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationProvider.overrideWith((ref) => _FakeNotificationNotifier(state)),
        ],
        child: const MaterialApp(
          home: NotificationsScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Notifications'), findsOneWidget);
    expect(find.text('2'), findsWidgets); // Badge count
    expect(find.text('Booking Confirmed: HIIT Blast'), findsOneWidget);
    expect(find.text('Facility Notice: Treadmill Repair'), findsOneWidget);
    expect(find.text('Membership Renewal Warning'), findsOneWidget);
    expect(find.text('Mark as read'), findsNWidgets(2)); // 2 unread items
  });

  testWidgets('NotificationsScreen displays empty view when no notifications exist', (tester) async {
    const state = NotificationState(
      isLoading: false,
      notifications: [],
      unreadCount: 0,
      unreadOnlyFilter: false,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationProvider.overrideWith((ref) => _FakeNotificationNotifier(state)),
        ],
        child: const MaterialApp(
          home: NotificationsScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('No Notifications Yet'), findsOneWidget);
  });

  testWidgets('NotificationsScreen triggers mark as read when button tapped', (tester) async {
    final fakeNotifier = _FakeNotificationNotifier(NotificationState(
      isLoading: false,
      notifications: sampleNotifications,
      unreadCount: 2,
    ));

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationProvider.overrideWith((ref) => fakeNotifier),
        ],
        child: const MaterialApp(
          home: NotificationsScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final markReadButtons = find.text('Mark as read');
    expect(markReadButtons, findsNWidgets(2));

    await tester.tap(markReadButtons.first);
    await tester.pumpAndSettle();

    expect(fakeNotifier.markedAsReadId, 'notif-1');
  });

  testWidgets('NotificationsScreen triggers mark all as read from app bar action', (tester) async {
    final fakeNotifier = _FakeNotificationNotifier(NotificationState(
      isLoading: false,
      notifications: sampleNotifications,
      unreadCount: 2,
    ));

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationProvider.overrideWith((ref) => fakeNotifier),
        ],
        child: const MaterialApp(
          home: NotificationsScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final markAllButton = find.text('Mark All Read');
    expect(markAllButton, findsOneWidget);

    await tester.tap(markAllButton);
    await tester.pumpAndSettle();

    expect(fakeNotifier.markAllAsReadCalled, isTrue);
  });
}

class _FakeNotificationNotifier extends StateNotifier<NotificationState>
    implements NotificationNotifier {
  String? markedAsReadId;
  bool markAllAsReadCalled = false;
  bool? filterChangedTo;

  _FakeNotificationNotifier(super.state);

  @override
  Future<void> loadNotifications({bool? unreadOnly}) async {}

  @override
  void setFilter(bool unreadOnly) {
    filterChangedTo = unreadOnly;
  }

  @override
  Future<void> refreshUnreadCount() async {}

  @override
  Future<void> markAsRead(String id) async {
    markedAsReadId = id;
  }

  @override
  Future<void> markAllAsRead() async {
    markAllAsReadCalled = true;
  }
}
