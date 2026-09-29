import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/class_schedule_model.dart';
import '../models/trainer_models.dart';
import '../providers/auth_provider.dart';
import 'api_service.dart';

final trainerApiServiceProvider = Provider<TrainerApiService>((ref) {
  final api = ref.watch(apiServiceProvider);
  return TrainerApiService(api);
});

class TrainerApiService {
  final ApiService _api;

  TrainerApiService(this._api);

  Future<List<ClassScheduleModel>> fetchAssignedSchedules() async {
    final response = await _api.get('/class-schedules?pageSize=50');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => ClassScheduleModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<List<ScheduleAttendeeModel>> fetchScheduleAttendees(String scheduleId) async {
    final response = await _api.get('/attendances/schedule/$scheduleId');
    if (response.statusCode == 200) {
      final items = jsonDecode(response.body) as List<dynamic>? ?? [];
      return items.map((e) => ScheduleAttendeeModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<bool> recordAttendance({
    required String bookingId,
    required String status,
    String? notes,
  }) async {
    final response = await _api.post('/attendances', {
      'bookingId': bookingId,
      'status': status,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
    });
    return response.statusCode == 200 || response.statusCode == 201;
  }

  Future<List<MemberGoalOverviewModel>> fetchMemberGoals(String memberId) async {
    final response = await _api.get('/goals?memberId=$memberId');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => MemberGoalOverviewModel.fromJson(e)).toList();
    }
    return [];
  }
}
