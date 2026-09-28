import 'dart:convert';
import 'package:http/http.dart' as http;
import 'secure_storage_service.dart';

class ApiService {
  final String baseUrl;
  final SecureStorageService _storage;

  ApiService({
    this.baseUrl = 'http://10.0.2.2:5000/api',
    SecureStorageService? storage,
  }) : _storage = storage ?? SecureStorageService();

  Future<Map<String, String>> _getHeaders() async {
    final token = await _storage.getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<http.Response> get(String endpoint) async {
    final uri = Uri.parse('$baseUrl$endpoint');
    final headers = await _getHeaders();
    return await http.get(uri, headers: headers);
  }

  Future<http.Response> post(String endpoint, Map<String, dynamic> body) async {
    final uri = Uri.parse('$baseUrl$endpoint');
    final headers = await _getHeaders();
    return await http.post(
      uri,
      headers: headers,
      body: jsonEncode(body),
    );
  }
}
