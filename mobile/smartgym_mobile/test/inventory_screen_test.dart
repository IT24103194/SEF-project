import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartgym_mobile/models/inventory_models.dart';
import 'package:smartgym_mobile/screens/inventory_screen.dart';
import 'package:smartgym_mobile/screens/stock_history_screen.dart';
import 'package:smartgym_mobile/services/inventory_api_service.dart';

class MockInventoryApiService extends InventoryApiService {
  final List<InventoryItem> mockItems;
  final List<StockMovement> mockMovements;

  MockInventoryApiService({
    required this.mockItems,
    required this.mockMovements,
  });

  @override
  Future<List<InventoryItem>> fetchInventory({
    bool? isLowStock,
    String? searchTerm,
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    var result = List<InventoryItem>.from(mockItems);
    if (isLowStock == true) {
      result = result.where((i) => i.isLowStock).toList();
    }
    if (searchTerm != null && searchTerm.isNotEmpty) {
      final s = searchTerm.toLowerCase();
      result = result
          .where((i) =>
              i.productName.toLowerCase().contains(s) ||
              i.productSKU.toLowerCase().contains(s) ||
              i.categoryName.toLowerCase().contains(s))
          .toList();
    }
    return result;
  }

  @override
  Future<List<StockMovement>> fetchHistory(String inventoryItemId) async {
    return mockMovements;
  }

  @override
  Future<InventoryItem> adjustStock({
    required String inventoryItemId,
    required int quantityChange,
    required String movementType,
    required String reason,
  }) async {
    final item = mockItems.firstWhere((i) => i.id == inventoryItemId);
    final newQty = item.quantityInStock + quantityChange;
    if (newQty < 0) {
      throw Exception('Stock cannot become negative.');
    }
    return item;
  }

  @override
  Future<Map<String, dynamic>> reorderProduct({
    required String inventoryItemId,
    required int quantity,
    String? notes,
  }) async {
    return {
      'orderNumber': 'PO-9999',
      'status': 'Submitted',
      'quantity': quantity,
    };
  }
}

void main() {
  final testItem1 = InventoryItem(
    id: 'item-1',
    productId: 'prod-1',
    productSKU: 'WHEY-100',
    productName: 'Gold Whey Protein 1kg',
    categoryName: 'Protein Supplements',
    supplierName: 'Optimum Nutrition',
    unitPrice: 59.99,
    costPrice: 30.00,
    quantityInStock: 4,
    reorderThreshold: 10,
    maxStockLevel: 100,
    locationBin: 'Bin-A1',
    isLowStock: true,
    updatedAt: DateTime.now(),
  );

  final testItem2 = InventoryItem(
    id: 'item-2',
    productId: 'prod-2',
    productSKU: 'BCAA-200',
    productName: 'BCAA Recovery Powder',
    categoryName: 'Amino Acids',
    supplierName: 'Scivation',
    unitPrice: 34.99,
    costPrice: 18.00,
    quantityInStock: 25,
    reorderThreshold: 10,
    maxStockLevel: 120,
    locationBin: 'Bin-C3',
    isLowStock: false,
    updatedAt: DateTime.now(),
  );

  final testMovement = StockMovement(
    id: 'mov-1',
    inventoryItemId: 'item-1',
    quantityChange: 15,
    movementType: 'Restock',
    reason: 'Initial warehouse intake',
    performedByUserName: 'Store Manager',
    createdAt: DateTime.now(),
  );

  test('InventoryItem and StockMovement JSON serialization works correctly', () {
    final json = {
      'id': 'test-id',
      'productId': 'prod-id',
      'productSKU': 'SKU-001',
      'productName': 'Test Protein',
      'categoryName': 'Supplements',
      'supplierName': 'Test Supplier',
      'unitPrice': 49.99,
      'costPrice': 25.00,
      'quantityInStock': 12,
      'reorderThreshold': 10,
      'maxStockLevel': 100,
      'locationBin': 'Bin-X',
      'isLowStock': false,
      'updatedAt': '2026-09-28T09:00:00Z',
    };

    final item = InventoryItem.fromJson(json);
    expect(item.id, 'test-id');
    expect(item.productSKU, 'SKU-001');
    expect(item.productName, 'Test Protein');
    expect(item.quantityInStock, 12);
    expect(item.isLowStock, false);
  });

  testWidgets('InventoryScreen displays inventory items and highlights low stock', (tester) async {
    final mockApi = MockInventoryApiService(
      mockItems: [testItem1, testItem2],
      mockMovements: [testMovement],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: InventoryScreen(apiService: mockApi),
      ),
    );

    // Initial pump & settle
    await tester.pumpAndSettle();

    // Verify items rendered
    expect(find.text('WHEY-100'), findsOneWidget);
    expect(find.text('Gold Whey Protein 1kg'), findsOneWidget);
    expect(find.text('BCAA-200'), findsOneWidget);

    // Verify Low Stock badge rendered in KPI and on item1 card (stock 4 <= threshold 10)
    expect(find.text('LOW STOCK'), findsNWidgets(2));
    expect(find.text('4 IN STOCK'), findsOneWidget);
    expect(find.text('25 IN STOCK'), findsOneWidget);
  });

  testWidgets('InventoryScreen opens stock adjustment bottom sheet with validation', (tester) async {
    final mockApi = MockInventoryApiService(
      mockItems: [testItem1],
      mockMovements: [testMovement],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: InventoryScreen(apiService: mockApi),
      ),
    );

    await tester.pumpAndSettle();

    // Tap Adjust button
    final adjustButton = find.widgetWithText(OutlinedButton, 'Adjust');
    expect(adjustButton, findsOneWidget);
    await tester.tap(adjustButton);
    await tester.pumpAndSettle();

    // Bottom sheet should be visible
    expect(find.text('Adjust Stock: Gold Whey Protein 1kg'), findsOneWidget);
    expect(find.text('Current balance: 4 units'), findsOneWidget);

    // Enter a deduction that causes negative stock (-10)
    final qtyField = find.widgetWithText(TextField, 'Quantity Change (+ for add, - for deduct)');
    await tester.enterText(qtyField, '-10');
    await tester.pump();

    // Verify negative stock violation warning is displayed
    expect(find.text('Business rule violation: Stock cannot become negative.'), findsOneWidget);

    // Submit button should be disabled
    final submitBtn = tester.widget<ElevatedButton>(find.widgetWithText(ElevatedButton, 'Apply Stock Adjustment'));
    expect(submitBtn.onPressed, isNull);
  });

  testWidgets('StockHistoryScreen displays historical movements timeline', (tester) async {
    final mockApi = MockInventoryApiService(
      mockItems: [testItem1],
      mockMovements: [testMovement],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: StockHistoryScreen(item: testItem1, apiService: mockApi),
      ),
    );

    await tester.pumpAndSettle();

    // Verify title and movement item
    expect(find.text('Stock Movement History'), findsOneWidget);
    expect(find.text('RESTOCK'), findsOneWidget);
    expect(find.text('+15'), findsOneWidget);
    expect(find.text('Initial warehouse intake'), findsOneWidget);
    expect(find.text('By: Store Manager'), findsOneWidget);
  });
}
