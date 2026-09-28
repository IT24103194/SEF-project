import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/membership_model.dart';
import '../services/membership_api_service.dart';

class MembershipState {
  final bool isLoading;
  final MembershipModel? activeMembership;
  final List<MembershipPlanModel> plans;
  final List<GoalModel> goals;
  final String? errorMessage;

  MembershipState({
    this.isLoading = false,
    this.activeMembership,
    this.plans = const [],
    this.goals = const [],
    this.errorMessage,
  });

  MembershipState copyWith({
    bool? isLoading,
    MembershipModel? activeMembership,
    List<MembershipPlanModel>? plans,
    List<GoalModel>? goals,
    String? errorMessage,
  }) {
    return MembershipState(
      isLoading: isLoading ?? this.isLoading,
      activeMembership: activeMembership ?? this.activeMembership,
      plans: plans ?? this.plans,
      goals: goals ?? this.goals,
      errorMessage: errorMessage,
    );
  }
}

class MembershipNotifier extends StateNotifier<MembershipState> {
  final MembershipApiService _service;

  MembershipNotifier(this._service) : super(MembershipState());

  Future<void> loadDashboard() async {
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      final membership = await _service.getMyMembership();
      final plans = await _service.getPlans();
      final goals = await _service.getMyGoals();

      state = state.copyWith(
        isLoading: false,
        activeMembership: membership,
        plans: plans,
        goals: goals,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
    }
  }

  Future<void> renew({required String planId, bool autoRenew = false}) async {
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      final renewed = await _service.renewMembership(planId: planId, autoRenew: autoRenew);
      state = state.copyWith(
        isLoading: false,
        activeMembership: renewed,
      );
      await loadDashboard();
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
      rethrow;
    }
  }

  Future<void> createGoal({
    required String title,
    required double targetValue,
    required double currentValue,
    required String unit,
    required DateTime targetDate,
  }) async {
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      await _service.createGoal(
        title: title,
        targetValue: targetValue,
        currentValue: currentValue,
        unit: unit,
        targetDate: targetDate,
      );
      await loadDashboard();
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
      rethrow;
    }
  }

  Future<void> recordProgress({
    required String goalId,
    required double value,
    String? notes,
  }) async {
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      await _service.recordProgress(
        goalId: goalId,
        value: value,
        notes: notes,
      );
      await loadDashboard();
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
      rethrow;
    }
  }

  Future<void> completeGoal(String goalId) async {
    try {
      await _service.completeGoal(goalId);
      await loadDashboard();
    } catch (e) {
      state = state.copyWith(
        errorMessage: e.toString().replaceAll('Exception: ', ''),
      );
      rethrow;
    }
  }
}

final membershipProvider =
    StateNotifierProvider<MembershipNotifier, MembershipState>((ref) {
  final service = ref.watch(membershipApiServiceProvider);
  return MembershipNotifier(service);
});
