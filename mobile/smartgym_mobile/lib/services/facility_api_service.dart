import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:http/http.dart' as http;
import '../models/facility_issue_model.dart';
import '../providers/auth_provider.dart';
import 'api_service.dart';

final facilityApiServiceProvider = Provider<FacilityApiService>((ref) {
  final api = ref.watch(apiServiceProvider);
  return FacilityApiService(api);
});

class FacilityApiService {
  final ApiService _api;

  FacilityApiService(this._api);

  Future<List<LocationModel>> fetchLocations() async {
    final response = await _api.get('/locations?pageSize=50');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => LocationModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<List<EquipmentModel>> fetchEquipment({String? locationId}) async {
    final query = locationId != null ? '?locationId=$locationId&pageSize=50' : '?pageSize=50';
    final response = await _api.get('/equipment$query');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => EquipmentModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<List<FacilityIssueModel>> fetchMemberIssues() async {
    final response = await _api.get('/facility-issues?pageSize=50');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => FacilityIssueModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<FacilityIssueModel?> fetchIssueDetails(String id) async {
    final response = await _api.get('/facility-issues/$id');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return FacilityIssueModel.fromJson(data);
    }
    return null;
  }

  Future<List<IssueHistoryModel>> fetchIssueHistory(String id) async {
    final response = await _api.get('/facility-issues/$id/history');
    if (response.statusCode == 200) {
      final items = jsonDecode(response.body) as List<dynamic>? ?? [];
      return items.map((e) => IssueHistoryModel.fromJson(e)).toList();
    }
    return [];
  }

  Future<bool> submitIssueWithImage({
    required String locationId,
    String? equipmentId,
    required String title,
    required String description,
    int severity = 2,
    String? imagePath,
    List<int>? imageBytes,
    String? fileName,
  }) async {
    final fields = {
      'locationId': locationId,
      if (equipmentId != null && equipmentId.isNotEmpty) 'equipmentId': equipmentId,
      'title': title,
      'description': description,
      'severity': severity.toString(),
    };

    http.MultipartFile? file;
    if (imagePath != null && imagePath.isNotEmpty) {
      file = await http.MultipartFile.fromPath('image', imagePath);
    } else if (imageBytes != null && fileName != null) {
      file = http.MultipartFile.fromBytes('image', imageBytes, filename: fileName);
    }

    final streamedResponse = await _api.postMultipart(
      '/facility-issues/with-image',
      fields,
      file: file,
    );

    final response = await http.Response.fromStream(streamedResponse);
    return response.statusCode == 200 || response.statusCode == 201;
  }

  Future<Map<String, dynamic>?> fetchWorkflowStatus(String issueId) async {
    final response = await _api.get('/facility-issues/$issueId/workflow');
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    return null;
  }
}
