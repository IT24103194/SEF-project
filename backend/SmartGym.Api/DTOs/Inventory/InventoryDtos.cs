using System.ComponentModel.DataAnnotations;
using SmartGym.Api.Entities;

namespace SmartGym.Api.DTOs.Inventory;

// ==========================================
// SUPPLIER DTOs
// ==========================================
public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSupplierRequest
{
    [Required(ErrorMessage = "Supplier name is required.")]
    [MaxLength(150, ErrorMessage = "Supplier name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Contact person cannot exceed 100 characters.")]
    public string? ContactPerson { get; set; }

    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    [MaxLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
    public string? Phone { get; set; }

    [MaxLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierRequest : CreateSupplierRequest
{
}

// ==========================================
// PRODUCT CATEGORY DTOs
// ==========================================
public class ProductCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

public class CreateProductCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;
}

// ==========================================
// PRODUCT DTOs
// ==========================================
public class ProductDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    
    // Inline inventory state
    public Guid? InventoryItemId { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
    public bool IsLowStock { get; set; }
    public string? LocationBin { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductRequest
{
    [Required(ErrorMessage = "Category is required.")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "SKU is required.")]
    [MaxLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [MaxLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 10000000, ErrorMessage = "Unit price must be non-negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 10000000, ErrorMessage = "Cost price must be non-negative.")]
    public decimal CostPrice { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Initial Inventory parameters
    [Range(0, 100000, ErrorMessage = "Initial stock cannot be negative.")]
    public int InitialStock { get; set; } = 0;

    [Range(0, 100000, ErrorMessage = "Reorder threshold cannot be negative.")]
    public int ReorderThreshold { get; set; } = 10;

    [Range(1, 100000, ErrorMessage = "Max stock level must be at least 1.")]
    public int MaxStockLevel { get; set; } = 100;

    [MaxLength(100, ErrorMessage = "Location bin cannot exceed 100 characters.")]
    public string? LocationBin { get; set; } = "Warehouse-A";
}

public class UpdateProductRequest
{
    [Required(ErrorMessage = "Category is required.")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "SKU is required.")]
    [MaxLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [MaxLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 10000000, ErrorMessage = "Unit price must be non-negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 10000000, ErrorMessage = "Cost price must be non-negative.")]
    public decimal CostPrice { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}

// ==========================================
// INVENTORY ITEM & STOCK DTOs
// ==========================================
public class InventoryItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
    public int MaxStockLevel { get; set; }
    public string? LocationBin { get; set; }
    public DateTime? LastRestockedAt { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StockAdjustmentRequest
{
    /// <summary>
    /// Amount to adjust. Positive for additions (Restock/Return), negative for deductions (Sale/Waste/Adjustment).
    /// </summary>
    [Required]
    public int QuantityChange { get; set; }

    public StockMovementType MovementType { get; set; } = StockMovementType.Adjustment;

    [Required(ErrorMessage = "Reason is required for auditing stock movements.")]
    [MaxLength(250, ErrorMessage = "Reason cannot exceed 250 characters.")]
    public string Reason { get; set; } = string.Empty;
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public int QuantityChange { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReorderRequest
{
    [Range(1, 100000, ErrorMessage = "Reorder quantity must be at least 1.")]
    public int Quantity { get; set; } = 20;

    public DateTime? ExpectedDeliveryDate { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

public class ReorderResponse
{
    public Guid PurchaseOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
}

// ==========================================
// CSV IMPORT & EXPORT DTOs
// ==========================================
public class CsvImportRowError
{
    public int RowNumber { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class CsvImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<CsvImportRowError> Errors { get; set; } = new();
}

public class InventoryQueryParameters : DTOs.Common.PagedRequest
{
    public bool? IsLowStock { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
}
