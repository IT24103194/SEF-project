import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/user_model.dart';
import 'package:smartgym_mobile/providers/auth_provider.dart';
import 'package:smartgym_mobile/routing/app_router.dart';
import 'package:smartgym_mobile/screens/home_screen.dart';
import 'package:smartgym_mobile/services/api_service.dart';
import 'package:smartgym_mobile/services/notification_api_service.dart';
import 'package:smartgym_mobile/services/secure_storage_service.dart';

class MockNotificationService extends NotificationApiService {
  MockNotificationService() : super(apiService: ApiService());

  @override
  Future<int> getUnreadCount() async => 3;
}

void main() {
  testWidgets('HomeScreen renders Member dashboard actions for Member role', (tester) async {
    const memberUser = UserModel(
      id: 'usr-1',
      email: 'member@smartgym.com',
      firstName: 'Emily',
      lastName: 'Blunt',
      role: 'Member',
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith((ref) => AuthNotifier(
                apiService: ApiService(),
                storage: SecureStorageService(),
              )..state = const AuthState(user: memberUser)),
          notificationApiServiceProvider.overrideWithValue(MockNotificationService()),
        ],
        child: MaterialApp(
          routes: AppRouter.routes,
          home: const HomeScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('SmartGym Mobile'), findsOneWidget);
    expect(find.text('Welcome back, Emily!'), findsOneWidget);
    expect(find.text('MEMBER'), findsOneWidget);
    expect(find.text('My Membership & Fitness Goals'), findsOneWidget);
    expect(find.text('Browse & Book Classes'), findsOneWidget);
    expect(find.text('Equipment & Facility Issues'), findsOneWidget);
    expect(find.text('Member Feedback & Ratings'), findsOneWidget);
    expect(find.text('Nutrition & Supplements Catalog'), findsOneWidget);
  });

  testWidgets('HomeScreen renders Trainer dashboard actions for Trainer role', (tester) async {
    const trainerUser = UserModel(
      id: 'usr-2',
      email: 'trainer@smartgym.com',
      firstName: 'David',
      lastName: 'Goggins',
      role: 'Trainer',
      roles: ['Trainer'],
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith((ref) => AuthNotifier(
                apiService: ApiService(),
                storage: SecureStorageService(),
              )..state = const AuthState(user: trainerUser)),
          notificationApiServiceProvider.overrideWithValue(MockNotificationService()),
        ],
        child: MaterialApp(
          routes: AppRouter.routes,
          home: const HomeScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('SmartGym Trainer'), findsOneWidget);
    expect(find.text('Welcome back, David!'), findsOneWidget);
    expect(find.text('TRAINER'), findsOneWidget);
    expect(find.text('Trainer Station & Schedules'), findsOneWidget);
    expect(find.text('Take Class Attendance'), findsOneWidget);
    expect(find.text('Facility & Equipment Tickets'), findsOneWidget);
  });
}
