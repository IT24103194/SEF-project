import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/membership_model.dart';
import 'package:smartgym_mobile/providers/membership_provider.dart';
import 'package:smartgym_mobile/screens/membership_screen.dart';

void main() {
  final samplePlans = [
    MembershipPlanModel(
      id: 'plan-1',
      name: 'Platinum All-Access',
      description: 'Full gym and unlimited classes',
      price: 89.99,
      durationDays: 30,
      maxClassesPerWeek: 7,
      hasTrainerAccess: true,
      isActive: true,
    ),
    MembershipPlanModel(
      id: 'plan-2',
      name: 'Basic Access',
      description: 'Gym floor access only',
      price: 39.99,
      durationDays: 30,
      maxClassesPerWeek: 0,
      hasTrainerAccess: false,
      isActive: true,
    ),
  ];

  final sampleMembership = MembershipModel(
    id: 'mem-1',
    memberId: 'member-1',
    memberName: 'Alex Rivera',
    memberEmail: 'alex@smartgym.com',
    planId: 'plan-1',
    planName: 'Platinum All-Access',
    startDate: DateTime.now().subtract(const Duration(days: 10)),
    endDate: DateTime.now().add(const Duration(days: 20)),
    status: 'Active',
    autoRenew: true,
    pricePaid: 89.99,
  );

  final sampleGoals = [
    GoalModel(
      id: 'goal-1',
      memberId: 'member-1',
      title: 'Bench Press 100kg',
      targetValue: 100,
      currentValue: 80,
      unit: 'kg',
      targetDate: DateTime.now().add(const Duration(days: 60)),
      status: 'InProgress',
      totalLogsCount: 2,
      recentLogs: [
        ProgressRecordModel(
          id: 'log-1',
          goalId: 'goal-1',
          recordedDate: DateTime.now().subtract(const Duration(days: 2)),
          value: 80,
          notes: 'Solid form',
        ),
      ],
    ),
  ];

  testWidgets('MembershipScreen renders active membership card details', (tester) async {
    final state = MembershipState(
      isLoading: false,
      activeMembership: sampleMembership,
      plans: samplePlans,
      goals: sampleGoals,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          membershipProvider.overrideWith((ref) => _FakeMembershipNotifier(state)),
        ],
        child: const MaterialApp(
          home: MembershipScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Membership & Goals'), findsOneWidget);
    expect(find.text('Platinum All-Access'), findsOneWidget);
    expect(find.text('Alex Rivera'), findsOneWidget);
    expect(find.text('alex@smartgym.com'), findsOneWidget);
    expect(find.text('Renew Membership'), findsOneWidget);
  });

  testWidgets('MembershipScreen opens renew bottom sheet when renew button tapped', (tester) async {
    final state = MembershipState(
      isLoading: false,
      activeMembership: sampleMembership,
      plans: samplePlans,
      goals: sampleGoals,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          membershipProvider.overrideWith((ref) => _FakeMembershipNotifier(state)),
        ],
        child: const MaterialApp(
          home: MembershipScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final renewButton = find.text('Renew Membership');
    expect(renewButton, findsOneWidget);

    await tester.tap(renewButton);
    await tester.pumpAndSettle();

    expect(find.text('Confirm Renewal'), findsOneWidget);
    expect(find.text('Select Plan'), findsOneWidget);
  });

  testWidgets('MembershipScreen switches to Goals tab and displays goals list', (tester) async {
    final state = MembershipState(
      isLoading: false,
      activeMembership: sampleMembership,
      plans: samplePlans,
      goals: sampleGoals,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          membershipProvider.overrideWith((ref) => _FakeMembershipNotifier(state)),
        ],
        child: const MaterialApp(
          home: MembershipScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Tap Goals & Progress tab
    final goalsTab = find.text('Goals & Progress');
    await tester.tap(goalsTab);
    await tester.pumpAndSettle();

    expect(find.text('Fitness Milestones'), findsOneWidget);
    expect(find.text('Bench Press 100kg'), findsOneWidget);
    expect(find.text('80%'), findsOneWidget);
    expect(find.text('Log Progress'), findsOneWidget);
  });

  testWidgets('MembershipScreen opens create goal dialog when New Goal tapped', (tester) async {
    final state = MembershipState(
      isLoading: false,
      activeMembership: sampleMembership,
      plans: samplePlans,
      goals: sampleGoals,
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          membershipProvider.overrideWith((ref) => _FakeMembershipNotifier(state)),
        ],
        child: const MaterialApp(
          home: MembershipScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Tap Goals & Progress tab
    final goalsTab = find.text('Goals & Progress');
    await tester.tap(goalsTab);
    await tester.pumpAndSettle();

    final newGoalBtn = find.text('New Goal');
    expect(newGoalBtn, findsOneWidget);

    await tester.tap(newGoalBtn);
    await tester.pumpAndSettle();

    expect(find.text('Set Fitness Goal'), findsOneWidget);
    expect(find.text('Save Goal'), findsOneWidget);
  });
}

class _FakeMembershipNotifier extends StateNotifier<MembershipState> implements MembershipNotifier {
  _FakeMembershipNotifier(super.state);

  @override
  Future<void> loadDashboard() async {}

  @override
  Future<void> renew({required String planId, bool autoRenew = false}) async {}

  @override
  Future<void> createGoal({
    required String title,
    required double targetValue,
    required double currentValue,
    required String unit,
    required DateTime targetDate,
  }) async {}

  @override
  Future<void> recordProgress({
    required String goalId,
    required double value,
    String? notes,
  }) async {}

  @override
  Future<void> completeGoal(String goalId) async {}
}
