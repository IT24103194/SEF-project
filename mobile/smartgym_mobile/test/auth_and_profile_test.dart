import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/membership_model.dart';
import 'package:smartgym_mobile/models/user_model.dart';
import 'package:smartgym_mobile/providers/auth_provider.dart';
import 'package:smartgym_mobile/screens/login_screen.dart';
import 'package:smartgym_mobile/screens/profile_screen.dart';
import 'package:smartgym_mobile/screens/register_screen.dart';
import 'package:smartgym_mobile/services/api_service.dart';
import 'package:smartgym_mobile/services/membership_api_service.dart';
import 'package:smartgym_mobile/services/secure_storage_service.dart';

class MockSecureStorage extends SecureStorageService {
  String? storedToken;
  String? storedUser;

  @override
  Future<void> saveToken(String token) async => storedToken = token;

  @override
  Future<String?> getToken() async => storedToken;

  @override
  Future<void> saveUserData(String userDataJson) async => storedUser = userDataJson;

  @override
  Future<String?> getUserData() async => storedUser;

  @override
  Future<void> clearAuth() async {
    storedToken = null;
    storedUser = null;
  }
}

class MockMembershipService extends MembershipApiService {
  MockMembershipService() : super(apiService: ApiService());

  @override
  Future<MembershipModel?> getMyMembership() async {
    return MembershipModel(
      id: 'mem-101',
      memberId: 'usr-999',
      memberName: 'Alex Champion',
      memberEmail: 'alex.champion@smartgym.com',
      planId: 'plan-1',
      planName: 'Elite Annual Gym Plan',
      status: 'Active',
      startDate: DateTime.now().subtract(const Duration(days: 30)),
      endDate: DateTime.now().add(const Duration(days: 335)),
      autoRenew: true,
      pricePaid: 599.99,
    );
  }

  @override
  Future<List<MembershipPlanModel>> getPlans() async => [];

  @override
  Future<List<GoalModel>> getMyGoals() async => [];
}

void main() {
  testWidgets('LoginScreen renders email, password fields and sign in button', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(
          home: LoginScreen(),
        ),
      ),
    );

    expect(find.text('SmartGym Mobile'), findsOneWidget);
    expect(find.text('Sign In to SmartGym'), findsOneWidget);
    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(find.text('Register'), findsOneWidget);
  });

  testWidgets('RegisterScreen validates required fields and password strength', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);

    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(
          home: RegisterScreen(),
        ),
      ),
    );

    expect(find.text('Create Account'), findsOneWidget);
    expect(find.text('Member'), findsOneWidget);
    expect(find.text('Trainer'), findsOneWidget);

    // Tap Register without filling form
    final registerBtn = find.text('Register Account');
    await tester.ensureVisible(registerBtn);
    await tester.tap(registerBtn);
    await tester.pumpAndSettle();

    expect(find.text('Required'), findsNWidgets(2)); // First Name, Last Name
    expect(find.text('Email is required'), findsOneWidget);
    expect(find.text('Password is required'), findsOneWidget);
  });

  testWidgets('RegisterScreen role switch between Member and Trainer', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);

    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(
          home: RegisterScreen(),
        ),
      ),
    );

    final memberFinder = find.text('Member');
    final trainerFinder = find.text('Trainer');
    expect(memberFinder, findsOneWidget);
    expect(trainerFinder, findsOneWidget);

    // Tap Trainer
    await tester.tap(trainerFinder);
    await tester.pumpAndSettle();

    // Tap Member
    await tester.tap(memberFinder);
    await tester.pumpAndSettle();
  });

  testWidgets('ProfileScreen renders user details, role badge, and membership validity', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);

    const mockUser = UserModel(
      id: 'usr-999',
      email: 'alex.champion@smartgym.com',
      firstName: 'Alex',
      lastName: 'Champion',
      role: 'Member',
      roles: ['Member'],
      phoneNumber: '+1-555-0199',
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith((ref) => AuthNotifier(
                apiService: ApiService(),
                storage: MockSecureStorage(),
              )..state = const AuthState(user: mockUser)),
          membershipApiServiceProvider.overrideWithValue(MockMembershipService()),
        ],
        child: const MaterialApp(
          home: ProfileScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Alex Champion'), findsOneWidget);
    expect(find.text('alex.champion@smartgym.com'), findsNWidgets(2));
    expect(find.text('MEMBER'), findsOneWidget);
    expect(find.text('Elite Annual Gym Plan'), findsOneWidget);
    expect(find.text('ACTIVE'), findsOneWidget);
    expect(find.text('Sign Out'), findsOneWidget);
  });

  testWidgets('ProfileScreen shows sign out confirmation dialog', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);

    const mockUser = UserModel(
      id: 'usr-999',
      email: 'alex@smartgym.com',
      firstName: 'Alex',
      lastName: 'Smith',
      role: 'Member',
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith((ref) => AuthNotifier(
                apiService: ApiService(),
                storage: MockSecureStorage(),
              )..state = const AuthState(user: mockUser)),
          membershipApiServiceProvider.overrideWithValue(MockMembershipService()),
        ],
        child: const MaterialApp(
          home: ProfileScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final signOutBtn = find.text('Sign Out');
    await tester.ensureVisible(signOutBtn);
    await tester.tap(signOutBtn);
    await tester.pumpAndSettle();

    expect(find.text('Are you sure you want to sign out of SmartGym?'), findsOneWidget);
    expect(find.text('Cancel'), findsOneWidget);
  });
}
