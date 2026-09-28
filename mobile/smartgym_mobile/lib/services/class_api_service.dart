import 'dart:convert';
import '../models/class_schedule_model.dart';
import 'api_service.dart';

class ClassApiService {
  final ApiService _apiService;

  ClassApiService({ApiService? apiService}) : _apiService = apiService ?? ApiService();

  Future<List<ClassScheduleModel>> getSchedules({DateTime? date}) async {
    String endpoint = '/class-schedules?page=1&pageSize=50';
    if (date != null) {
      final dateStr = date.toIso8601String().split('T')[0];
      endpoint += '&startDate=${dateStr}T00:00:00Z&endDate=${dateStr}T23:59:59Z';
    }

    final response = await _apiService.get(endpoint);
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = (data['items'] as List<dynamic>?) ?? [];
      return items.map((json) => ClassScheduleModel.fromJson(json)).toList();
    }
    throw Exception('Failed to load class schedules: ${response.statusCode}');
  }

  Future<List<ClassBookingModel>> getMyBookings({bool? upcomingOnly}) async {
    String endpoint = '/bookings/my-bookings';
    if (upcomingOnly == true) {
      endpoint += '?upcomingOnly=true';
    }

    final response = await _apiService.get(endpoint);
    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.map((json) => ClassBookingModel.fromJson(json)).toList();
    }
    throw Exception('Failed to load bookings: ${response.statusCode}');
  }

  Future<ClassBookingModel> bookClass(String scheduleId) async {
    final response = await _apiService.post('/bookings', {
      'scheduleId': scheduleId,
    });

    if (response.statusCode == 200 || response.statusCode == 201) {
      return ClassBookingModel.fromJson(jsonDecode(response.body));
    }

    final err = jsonDecode(response.body);
    final message = err['detail'] ?? err['title'] ?? 'Unable to book class';
    throw Exception(message);
  }

  Future<ClassBookingModel> cancelBooking(String bookingId, {String? reason}) async {
    final response = await _apiService.post('/bookings/$bookingId/cancel', {
      'reason': reason ?? 'Member requested cancellation via mobile app',
    });

    if (response.statusCode == 200) {
      return ClassBookingModel.fromJson(jsonDecode(response.body));
    }

    final err = jsonDecode(response.body);
    final message = err['detail'] ?? err['title'] ?? 'Unable to cancel booking';
    throw Exception(message);
  }
}
