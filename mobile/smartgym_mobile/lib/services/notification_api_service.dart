import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/notification_model.dart';
import 'api_service.dart';

final notificationApiServiceProvider = Provider<NotificationApiService>((ref) {
  return NotificationApiService();
});

class NotificationApiService {
  final ApiService _apiService;

  NotificationApiService({ApiService? apiService})
      : _apiService = apiService ?? ApiService();

  Future<List<NotificationModel>> getNotifications({
    bool? unreadOnly,
    int page = 1,
    int pageSize = 20,
  }) async {
    final query = <String>[
      'page=$page',
      'pageSize=$pageSize',
      if (unreadOnly != null) 'unreadOnly=$unreadOnly',
    ].join('&');

    final response = await _apiService.get('/notifications?$query');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final List<dynamic> items = data['items'] ?? [];
      return items.map((json) => NotificationModel.fromJson(json)).toList();
    }
    throw Exception('Failed to load notifications: ${response.statusCode}');
  }

  Future<int> getUnreadCount() async {
    final response = await _apiService.get('/notifications/unread-count');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return data['unreadCount'] ?? 0;
    }
    return 0;
  }

  Future<NotificationSummaryModel> getSummary() async {
    final response = await _apiService.get('/notifications/summary');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return NotificationSummaryModel.fromJson(data);
    }
    throw Exception('Failed to load notification summary');
  }

  Future<NotificationModel> markAsRead(String id) async {
    final response = await _apiService.post('/notifications/$id/read', {});
    if (response.statusCode == 200) {
      return NotificationModel.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to mark notification as read');
  }

  Future<int> markAllAsRead() async {
    final response = await _apiService.post('/notifications/read-all', {});
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return data['markedCount'] ?? 0;
    }
    return 0;
  }
}
