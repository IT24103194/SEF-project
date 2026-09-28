class InventoryItem {
  final String id;
  final String productId;
  final String productSKU;
  final String productName;
  final String categoryName;
  final String supplierName;
  final double unitPrice;
  final double costPrice;
  final int quantityInStock;
  final int reorderThreshold;
  final int maxStockLevel;
  final String? locationBin;
  final bool isLowStock;
  final DateTime updatedAt;

  InventoryItem({
    required this.id,
    required this.productId,
    required this.productSKU,
    required this.productName,
    required this.categoryName,
    required this.supplierName,
    required this.unitPrice,
    required this.costPrice,
    required this.quantityInStock,
    required this.reorderThreshold,
    required this.maxStockLevel,
    this.locationBin,
    required this.isLowStock,
    required this.updatedAt,
  });

  factory InventoryItem.fromJson(Map<String, dynamic> json) {
    return InventoryItem(
      id: json['id'] as String,
      productId: json['productId'] as String,
      productSKU: json['productSKU'] as String? ?? json['productSku'] as String? ?? '',
      productName: json['productName'] as String? ?? '',
      categoryName: json['categoryName'] as String? ?? '',
      supplierName: json['supplierName'] as String? ?? '',
      unitPrice: (json['unitPrice'] as num?)?.toDouble() ?? 0.0,
      costPrice: (json['costPrice'] as num?)?.toDouble() ?? 0.0,
      quantityInStock: json['quantityInStock'] as int? ?? 0,
      reorderThreshold: json['reorderThreshold'] as int? ?? 10,
      maxStockLevel: json['maxStockLevel'] as int? ?? 100,
      locationBin: json['locationBin'] as String?,
      isLowStock: json['isLowStock'] as bool? ?? false,
      updatedAt: json['updatedAt'] != null
          ? DateTime.parse(json['updatedAt'] as String)
          : DateTime.now(),
    );
  }
}

class StockMovement {
  final String id;
  final String inventoryItemId;
  final int quantityChange;
  final String movementType;
  final String reason;
  final String? performedByUserName;
  final DateTime createdAt;

  StockMovement({
    required this.id,
    required this.inventoryItemId,
    required this.quantityChange,
    required this.movementType,
    required this.reason,
    this.performedByUserName,
    required this.createdAt,
  });

  factory StockMovement.fromJson(Map<String, dynamic> json) {
    return StockMovement(
      id: json['id'] as String,
      inventoryItemId: json['inventoryItemId'] as String,
      quantityChange: json['quantityChange'] as int? ?? 0,
      movementType: json['movementType'] as String? ?? 'Adjustment',
      reason: json['reason'] as String? ?? '',
      performedByUserName: json['performedByUserName'] as String?,
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'] as String)
          : DateTime.now(),
    );
  }
}

class Supplier {
  final String id;
  final String name;
  final String? contactPerson;
  final String email;
  final String phone;
  final String? address;
  final bool isActive;
  final int productCount;

  Supplier({
    required this.id,
    required this.name,
    this.contactPerson,
    required this.email,
    required this.phone,
    this.address,
    required this.isActive,
    required this.productCount,
  });

  factory Supplier.fromJson(Map<String, dynamic> json) {
    return Supplier(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      contactPerson: json['contactPerson'] as String?,
      email: json['email'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      address: json['address'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      productCount: json['productCount'] as int? ?? 0,
    );
  }
}
