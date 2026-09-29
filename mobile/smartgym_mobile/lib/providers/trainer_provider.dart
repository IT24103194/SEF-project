import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/class_schedule_model.dart';
import '../models/trainer_models.dart';
import '../services/trainer_api_service.dart';

final trainerSchedulesProvider = FutureProvider.autoDispose<List<ClassScheduleModel>>((ref) async {
  final api = ref.watch(trainerApiServiceProvider);
  return await api.fetchAssignedSchedules();
});

final scheduleAttendeesProvider = FutureProvider.autoDispose.family<List<ScheduleAttendeeModel>, String>((ref, scheduleId) async {
  final api = ref.watch(trainerApiServiceProvider);
  return await api.fetchScheduleAttendees(scheduleId);
});

final memberGoalsOverviewProvider = FutureProvider.autoDispose.family<List<MemberGoalOverviewModel>, String>((ref, memberId) async {
  final api = ref.watch(trainerApiServiceProvider);
  return await api.fetchMemberGoals(memberId);
});
