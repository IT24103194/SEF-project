import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/facility_issue_model.dart';
import 'auth_provider.dart';

final facilityIssuesProvider = FutureProvider<List<FacilityIssueModel>>((ref) async {
  final api = ref.watch(apiServiceProvider);
  try {
    final response = await api.get('/facility-issues');
    if (response.statusCode == 200) {
      final List<dynamic> list = jsonDecode(response.body);
      return list.map((item) => FacilityIssueModel.fromJson(item)).toList();
    }
    return [];
  } catch (_) {
    return [];
  }
});
