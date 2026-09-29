import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/feedback_model.dart';
import '../providers/auth_provider.dart';
import 'api_service.dart';

final feedbackApiServiceProvider = Provider<FeedbackApiService>((ref) {
  final api = ref.watch(apiServiceProvider);
  return FeedbackApiService(api);
});

class FeedbackApiService {
  final ApiService _api;

  FeedbackApiService(this._api);

  Future<List<FeedbackModel>> fetchFeedbacks() async {
    final response = await _api.get('/feedback?pageSize=50');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => FeedbackModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<bool> submitFeedback({
    required String subject,
    required String content,
    required int rating,
  }) async {
    final response = await _api.post('/feedback', {
      'subject': subject,
      'content': content,
      'rating': rating,
    });
    return response.statusCode == 200 || response.statusCode == 201;
  }
}
