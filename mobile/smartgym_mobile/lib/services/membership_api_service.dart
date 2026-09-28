import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/membership_model.dart';
import 'api_service.dart';

final membershipApiServiceProvider = Provider<MembershipApiService>((ref) {
  return MembershipApiService();
});

class MembershipApiService {
  final ApiService _apiService;

  MembershipApiService({ApiService? apiService})
      : _apiService = apiService ?? ApiService();

  Future<MembershipModel?> getMyMembership() async {
    final response = await _apiService.get('/memberships/my-membership');
    if (response.statusCode == 200) {
      if (response.body.isEmpty || response.body == 'null') return null;
      final data = jsonDecode(response.body);
      if (data == null) return null;
      return MembershipModel.fromJson(data);
    }
    return null;
  }

  Future<List<MembershipPlanModel>> getPlans() async {
    final response = await _apiService.get('/membership-plans?includeInactive=false');
    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.map((json) => MembershipPlanModel.fromJson(json)).toList();
    }
    throw Exception('Failed to load membership plans: ${response.statusCode}');
  }

  Future<MembershipModel> renewMembership({
    required String planId,
    bool autoRenew = false,
  }) async {
    final response = await _apiService.post('/memberships/renew', {
      'planId': planId,
      'autoRenew': autoRenew,
    });

    if (response.statusCode == 200) {
      return MembershipModel.fromJson(jsonDecode(response.body));
    }
    final errorData = jsonDecode(response.body);
    throw Exception(errorData['detail'] ?? errorData['message'] ?? 'Failed to renew membership');
  }

  Future<List<GoalModel>> getMyGoals() async {
    final response = await _apiService.get('/goals/my-goals');
    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.map((json) => GoalModel.fromJson(json)).toList();
    }
    throw Exception('Failed to load goals: ${response.statusCode}');
  }

  Future<GoalModel> createGoal({
    required String title,
    required double targetValue,
    required double currentValue,
    required String unit,
    required DateTime targetDate,
  }) async {
    final response = await _apiService.post('/goals', {
      'title': title,
      'targetValue': targetValue,
      'currentValue': currentValue,
      'unit': unit,
      'targetDate': targetDate.toIso8601String(),
    });

    if (response.statusCode == 201) {
      return GoalModel.fromJson(jsonDecode(response.body));
    }
    final errorData = jsonDecode(response.body);
    throw Exception(errorData['detail'] ?? errorData['message'] ?? 'Failed to create goal');
  }

  Future<ProgressRecordModel> recordProgress({
    required String goalId,
    required double value,
    String? notes,
  }) async {
    final response = await _apiService.post('/progress-records', {
      'goalId': goalId,
      'value': value,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
    });

    if (response.statusCode == 201) {
      return ProgressRecordModel.fromJson(jsonDecode(response.body));
    }
    final errorData = jsonDecode(response.body);
    throw Exception(errorData['detail'] ?? errorData['message'] ?? 'Failed to record progress');
  }

  Future<List<ProgressRecordModel>> getGoalProgress(String goalId) async {
    final response = await _apiService.get('/goals/$goalId/progress');
    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.map((json) => ProgressRecordModel.fromJson(json)).toList();
    }
    throw Exception('Failed to load goal progress history');
  }

  Future<GoalModel> completeGoal(String goalId) async {
    final response = await _apiService.post('/goals/$goalId/complete', {});
    if (response.statusCode == 200) {
      return GoalModel.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to complete goal');
  }
}
