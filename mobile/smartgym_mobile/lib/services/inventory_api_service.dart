import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/inventory_models.dart';
import 'api_service.dart';
import 'secure_storage_service.dart';

class InventoryApiService {
  final ApiService _api;
  final String baseUrl;
  final SecureStorageService _storage;

  InventoryApiService({
    ApiService? api,
    this.baseUrl = 'http://10.0.2.2:5000/api',
    SecureStorageService? storage,
  })  : _api = api ?? ApiService(baseUrl: baseUrl, storage: storage),
        _storage = storage ?? SecureStorageService();

  Future<Map<String, String>> _getHeaders() async {
    final token = await _storage.getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<List<InventoryItem>> fetchInventory({
    bool? isLowStock,
    String? searchTerm,
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    final queryParams = <String, String>{
      'pageNumber': pageNumber.toString(),
      'pageSize': pageSize.toString(),
      if (isLowStock != null) 'isLowStock': isLowStock.toString(),
      if (searchTerm != null && searchTerm.isNotEmpty) 'searchTerm': searchTerm,
    };

    final uri = Uri.parse('$baseUrl/inventory').replace(queryParameters: queryParams);
    final headers = await _getHeaders();
    final response = await http.get(uri, headers: headers);

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      final itemsJson = data['items'] as List<dynamic>? ?? [];
      return itemsJson.map((i) => InventoryItem.fromJson(i as Map<String, dynamic>)).toList();
    } else {
      throw Exception('Failed to load inventory: ${response.statusCode}');
    }
  }

  Future<InventoryItem> fetchInventoryItem(String id) async {
    final response = await _api.get('/inventory/$id');
    if (response.statusCode == 200) {
      return InventoryItem.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    } else {
      throw Exception('Failed to fetch inventory item details: ${response.statusCode}');
    }
  }

  Future<InventoryItem> adjustStock({
    required String inventoryItemId,
    required int quantityChange,
    required String movementType,
    required String reason,
  }) async {
    final response = await _api.post('/inventory/$inventoryItemId/adjust', {
      'quantityChange': quantityChange,
      'movementType': movementType,
      'reason': reason,
    });

    if (response.statusCode == 200) {
      return InventoryItem.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    } else {
      final errorBody = jsonDecode(response.body);
      final msg = errorBody['detail'] ?? errorBody['message'] ?? 'Failed to adjust stock';
      throw Exception(msg);
    }
  }

  Future<Map<String, dynamic>> reorderProduct({
    required String inventoryItemId,
    required int quantity,
    String? notes,
  }) async {
    final response = await _api.post('/inventory/$inventoryItemId/reorder', {
      'quantity': quantity,
      if (notes != null) 'notes': notes,
    });

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      final errorBody = jsonDecode(response.body);
      final msg = errorBody['detail'] ?? errorBody['message'] ?? 'Failed to place reorder';
      throw Exception(msg);
    }
  }

  Future<List<StockMovement>> fetchHistory(String inventoryItemId) async {
    final response = await _api.get('/inventory/$inventoryItemId/history');
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list.map((e) => StockMovement.fromJson(e as Map<String, dynamic>)).toList();
    } else {
      throw Exception('Failed to fetch movement history: ${response.statusCode}');
    }
  }

  Future<List<Supplier>> fetchSuppliers() async {
    final response = await _api.get('/suppliers?pageSize=100');
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      final items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => Supplier.fromJson(e as Map<String, dynamic>)).toList();
    } else {
      throw Exception('Failed to fetch suppliers: ${response.statusCode}');
    }
  }
}
