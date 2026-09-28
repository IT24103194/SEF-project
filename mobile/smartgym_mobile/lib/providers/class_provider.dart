import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/class_schedule_model.dart';
import '../services/class_api_service.dart';

final classApiServiceProvider = Provider<ClassApiService>((ref) {
  return ClassApiService();
});

final classSchedulesProvider = FutureProvider.autoDispose<List<ClassScheduleModel>>((ref) async {
  final api = ref.watch(classApiServiceProvider);
  return await api.getSchedules();
});

final myBookingsProvider = FutureProvider.autoDispose<List<ClassBookingModel>>((ref) async {
  final api = ref.watch(classApiServiceProvider);
  return await api.getMyBookings();
});
