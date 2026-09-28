using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Inventory;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface IInventoryService
{
    // Suppliers
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task DeleteSupplierAsync(Guid id, CancellationToken cancellationToken = default);

    // Product Categories
    Task<List<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductCategoryDto> CreateCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default);

    // Products
    Task<PagedResult<ProductDto>> GetProductsAsync(PagedRequest request, Guid? categoryId = null, Guid? supplierId = null, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);

    // Inventory Items & Stock Movements
    Task<PagedResult<InventoryItemDto>> GetInventoryItemsAsync(InventoryQueryParameters query, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> GetInventoryItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> AdjustStockAsync(Guid inventoryItemId, StockAdjustmentRequest request, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<List<StockMovementDto>> GetStockHistoryAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);
    Task<ReorderResponse> ReorderProductAsync(Guid inventoryItemId, ReorderRequest request, Guid? userId = null, CancellationToken cancellationToken = default);

    // CSV Import / Export
    Task<byte[]> ExportInventoryCsvAsync(CancellationToken cancellationToken = default);
    Task<CsvImportResult> ImportInventoryCsvAsync(Stream csvStream, Guid? userId = null, CancellationToken cancellationToken = default);
}

public class InventoryService : IInventoryService
{
    private readonly SmartGymDbContext _dbContext;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        SmartGymDbContext dbContext,
        ITransactionService transactionService,
        ILogger<InventoryService> logger)
    {
        _dbContext = dbContext;
        _transactionService = transactionService;
        _logger = logger;
    }

    // ==========================================
    // SUPPLIERS
    // ==========================================

    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers
            .AsNoTracking()
            .Include(s => s.Products)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search) 
                                  || (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(search))
                                  || (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == SortDirection.Descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "createdat" => request.SortDirection == SortDirection.Descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                IsActive = s.IsActive,
                ProductCount = s.Products.Count,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<SupplierDto> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _dbContext.Suppliers
            .AsNoTracking()
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{id}' was not found.");
        }

        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            IsActive = supplier.IsActive,
            ProductCount = supplier.Products.Count,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Address = request.Address?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetSupplierByIdAsync(supplier.Id, cancellationToken);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{id}' was not found.");
        }

        supplier.Name = request.Name.Trim();
        supplier.ContactPerson = request.ContactPerson?.Trim();
        supplier.Email = request.Email?.Trim() ?? string.Empty;
        supplier.Phone = request.Phone?.Trim() ?? string.Empty;
        supplier.Address = request.Address?.Trim();
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetSupplierByIdAsync(supplier.Id, cancellationToken);
    }

    public async Task DeleteSupplierAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _dbContext.Suppliers
            .Include(s => s.Products)
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{id}' was not found.");
        }

        if (supplier.Products.Any())
        {
            throw new InvalidOperationException($"Cannot delete supplier '{supplier.Name}' because it has {supplier.Products.Count} linked products. Please reassign or delete the products first.");
        }

        if (supplier.PurchaseOrders.Any())
        {
            throw new InvalidOperationException($"Cannot delete supplier '{supplier.Name}' because it is associated with historical purchase orders.");
        }

        _dbContext.Suppliers.Remove(supplier);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // PRODUCT CATEGORIES
    // ==========================================

    public async Task<List<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductCategories
            .AsNoTracking()
            .Select(c => new ProductCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count
            })
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductCategoryDto> CreateCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ProductCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower(), cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"Category '{request.Name}' already exists.");
        }

        var cat = new ProductCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.ProductCategories.AddAsync(cat, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductCategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            ProductCount = 0
        };
    }

    // ==========================================
    // PRODUCTS
    // ==========================================

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        PagedRequest request,
        Guid? categoryId = null,
        Guid? supplierId = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.InventoryItem)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) 
                                  || p.SKU.ToLower().Contains(search)
                                  || p.Category.Name.ToLower().Contains(search)
                                  || p.Supplier.Name.ToLower().Contains(search));
        }

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == SortDirection.Descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "sku" => request.SortDirection == SortDirection.Descending ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
            "unitprice" => request.SortDirection == SortDirection.Descending ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
            "createdat" => request.SortDirection == SortDirection.Descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                SKU = p.SKU,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                CostPrice = p.CostPrice,
                ExpiryDate = p.ExpiryDate,
                IsActive = p.IsActive,
                InventoryItemId = p.InventoryItem != null ? p.InventoryItem.Id : null,
                QuantityInStock = p.InventoryItem != null ? p.InventoryItem.QuantityInStock : 0,
                ReorderThreshold = p.InventoryItem != null ? p.InventoryItem.ReorderThreshold : 0,
                IsLowStock = p.InventoryItem != null && p.InventoryItem.QuantityInStock <= p.InventoryItem.ReorderThreshold,
                LocationBin = p.InventoryItem != null ? p.InventoryItem.LocationBin : null,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (p == null)
        {
            throw new KeyNotFoundException($"Product with ID '{id}' was not found.");
        }

        return new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier.Name,
            SKU = p.SKU,
            Name = p.Name,
            Description = p.Description,
            UnitPrice = p.UnitPrice,
            CostPrice = p.CostPrice,
            ExpiryDate = p.ExpiryDate,
            IsActive = p.IsActive,
            InventoryItemId = p.InventoryItem?.Id,
            QuantityInStock = p.InventoryItem?.QuantityInStock ?? 0,
            ReorderThreshold = p.InventoryItem?.ReorderThreshold ?? 0,
            IsLowStock = p.InventoryItem != null && p.InventoryItem.QuantityInStock <= p.InventoryItem.ReorderThreshold,
            LocationBin = p.InventoryItem?.LocationBin,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        // 1. Validate Category and Supplier exist
        var categoryExists = await _dbContext.ProductCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new KeyNotFoundException($"Category with ID '{request.CategoryId}' does not exist.");
        }

        var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw new KeyNotFoundException($"Supplier with ID '{request.SupplierId}' does not exist.");
        }

        // 2. Validate SKU uniqueness
        var skuUpper = request.SKU.Trim().ToUpperInvariant();
        var skuExists = await _dbContext.Products.AnyAsync(p => p.SKU.ToUpper() == skuUpper, cancellationToken);
        if (skuExists)
        {
            throw new InvalidOperationException($"Product with SKU '{request.SKU}' already exists.");
        }

        // 3. Atomically create Product, InventoryItem, and initial StockMovement via TransactionService
        return await _transactionService.ExecuteInTransactionAsync(async () =>
        {
            var product = new Product
            {
                CategoryId = request.CategoryId,
                SupplierId = request.SupplierId,
                SKU = skuUpper,
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                UnitPrice = request.UnitPrice,
                CostPrice = request.CostPrice,
                ExpiryDate = request.ExpiryDate.HasValue ? DateTime.SpecifyKind(request.ExpiryDate.Value, DateTimeKind.Utc) : null,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Products.AddAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var inventoryItem = new InventoryItem
            {
                ProductId = product.Id,
                QuantityInStock = request.InitialStock,
                ReorderThreshold = request.ReorderThreshold,
                MaxStockLevel = request.MaxStockLevel,
                LocationBin = request.LocationBin?.Trim() ?? "Warehouse-A",
                LastRestockedAt = request.InitialStock > 0 ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.InventoryItems.AddAsync(inventoryItem, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.InitialStock > 0)
            {
                var movement = new StockMovement
                {
                    InventoryItemId = inventoryItem.Id,
                    QuantityChange = request.InitialStock,
                    MovementType = StockMovementType.Restock,
                    Reason = "Initial inventory setup upon product creation",
                    PerformedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return await GetProductByIdAsync(product.Id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{id}' was not found.");
        }

        var skuUpper = request.SKU.Trim().ToUpperInvariant();
        if (product.SKU != skuUpper)
        {
            var skuExists = await _dbContext.Products.AnyAsync(p => p.SKU.ToUpper() == skuUpper && p.Id != id, cancellationToken);
            if (skuExists)
            {
                throw new InvalidOperationException($"Product with SKU '{request.SKU}' already exists.");
            }
        }

        var categoryExists = await _dbContext.ProductCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new KeyNotFoundException($"Category with ID '{request.CategoryId}' does not exist.");
        }

        var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw new KeyNotFoundException($"Supplier with ID '{request.SupplierId}' does not exist.");
        }

        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.SKU = skuUpper;
        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.UnitPrice = request.UnitPrice;
        product.CostPrice = request.CostPrice;
        product.ExpiryDate = request.ExpiryDate.HasValue ? DateTime.SpecifyKind(request.ExpiryDate.Value, DateTimeKind.Utc) : null;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetProductByIdAsync(product.Id, cancellationToken);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.InventoryItem)
                .ThenInclude(i => i!.StockMovements)
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{id}' was not found.");
        }

        if (product.PurchaseOrderItems.Any())
        {
            throw new InvalidOperationException($"Cannot delete product '{product.Name}' because it has associated purchase order history.");
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // INVENTORY ITEMS & MOVEMENTS
    // ==========================================

    public async Task<PagedResult<InventoryItemDto>> GetInventoryItemsAsync(InventoryQueryParameters query, CancellationToken cancellationToken = default)
    {
        var itemsQuery = _dbContext.InventoryItems
            .AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Include(i => i.Product)
                .ThenInclude(p => p.Supplier)
            .AsQueryable();

        if (query.IsLowStock.HasValue && query.IsLowStock.Value)
        {
            itemsQuery = itemsQuery.Where(i => i.QuantityInStock <= i.ReorderThreshold);
        }

        if (query.CategoryId.HasValue)
        {
            itemsQuery = itemsQuery.Where(i => i.Product.CategoryId == query.CategoryId.Value);
        }

        if (query.SupplierId.HasValue)
        {
            itemsQuery = itemsQuery.Where(i => i.Product.SupplierId == query.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            itemsQuery = itemsQuery.Where(i => i.Product.Name.ToLower().Contains(search)
                                            || i.Product.SKU.ToLower().Contains(search)
                                            || i.Product.Category.Name.ToLower().Contains(search)
                                            || i.Product.Supplier.Name.ToLower().Contains(search)
                                            || (i.LocationBin != null && i.LocationBin.ToLower().Contains(search)));
        }

        itemsQuery = query.SortBy?.ToLower() switch
        {
            "productname" => query.SortDirection == SortDirection.Descending ? itemsQuery.OrderByDescending(i => i.Product.Name) : itemsQuery.OrderBy(i => i.Product.Name),
            "sku" => query.SortDirection == SortDirection.Descending ? itemsQuery.OrderByDescending(i => i.Product.SKU) : itemsQuery.OrderBy(i => i.Product.SKU),
            "quantityinstock" => query.SortDirection == SortDirection.Descending ? itemsQuery.OrderByDescending(i => i.QuantityInStock) : itemsQuery.OrderBy(i => i.QuantityInStock),
            "reorderthreshold" => query.SortDirection == SortDirection.Descending ? itemsQuery.OrderByDescending(i => i.ReorderThreshold) : itemsQuery.OrderBy(i => i.ReorderThreshold),
            "updatedat" => query.SortDirection == SortDirection.Descending ? itemsQuery.OrderByDescending(i => i.UpdatedAt) : itemsQuery.OrderBy(i => i.UpdatedAt),
            _ => itemsQuery.OrderBy(i => i.Product.Name)
        };

        var totalCount = await itemsQuery.CountAsync(cancellationToken);
        var items = await itemsQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new InventoryItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSKU = i.Product.SKU,
                ProductName = i.Product.Name,
                CategoryName = i.Product.Category.Name,
                SupplierName = i.Product.Supplier.Name,
                UnitPrice = i.Product.UnitPrice,
                CostPrice = i.Product.CostPrice,
                QuantityInStock = i.QuantityInStock,
                ReorderThreshold = i.ReorderThreshold,
                MaxStockLevel = i.MaxStockLevel,
                LocationBin = i.LocationBin,
                LastRestockedAt = i.LastRestockedAt,
                IsLowStock = i.QuantityInStock <= i.ReorderThreshold,
                UpdatedAt = i.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryItemDto>(items, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<InventoryItemDto> GetInventoryItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Include(i => i.Product)
                .ThenInclude(p => p.Supplier)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item == null)
        {
            throw new KeyNotFoundException($"Inventory item with ID '{id}' was not found.");
        }

        return new InventoryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductSKU = item.Product.SKU,
            ProductName = item.Product.Name,
            CategoryName = item.Product.Category.Name,
            SupplierName = item.Product.Supplier.Name,
            UnitPrice = item.Product.UnitPrice,
            CostPrice = item.Product.CostPrice,
            QuantityInStock = item.QuantityInStock,
            ReorderThreshold = item.ReorderThreshold,
            MaxStockLevel = item.MaxStockLevel,
            LocationBin = item.LocationBin,
            LastRestockedAt = item.LastRestockedAt,
            IsLowStock = item.QuantityInStock <= item.ReorderThreshold,
            UpdatedAt = item.UpdatedAt
        };
    }

    public async Task<InventoryItemDto> AdjustStockAsync(
        Guid inventoryItemId,
        StockAdjustmentRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var (success, message, _) = await _transactionService.ExecuteStockMovementAtomicAsync(
            inventoryItemId,
            request.QuantityChange,
            request.MovementType,
            request.Reason,
            userId,
            cancellationToken);

        if (!success)
        {
            throw new ArgumentException(message);
        }

        return await GetInventoryItemByIdAsync(inventoryItemId, cancellationToken);
    }

    public async Task<List<StockMovementDto>> GetStockHistoryAsync(Guid inventoryItemId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.InventoryItems.AnyAsync(i => i.Id == inventoryItemId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Inventory item with ID '{inventoryItemId}' was not found.");
        }

        var movements = await _dbContext.StockMovements
            .AsNoTracking()
            .Where(sm => sm.InventoryItemId == inventoryItemId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);

        // Fetch performing user names
        var userIds = movements.Where(m => m.PerformedByUserId.HasValue).Select(m => m.PerformedByUserId!.Value).Distinct().ToList();
        var users = await _dbContext.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);

        return movements.Select(sm => new StockMovementDto
        {
            Id = sm.Id,
            InventoryItemId = sm.InventoryItemId,
            QuantityChange = sm.QuantityChange,
            MovementType = sm.MovementType.ToString(),
            Reason = sm.Reason,
            PerformedByUserId = sm.PerformedByUserId,
            PerformedByUserName = sm.PerformedByUserId.HasValue && users.TryGetValue(sm.PerformedByUserId.Value, out var name) ? name : "System / Administrator",
            CreatedAt = sm.CreatedAt
        }).ToList();
    }

    public async Task<ReorderResponse> ReorderProductAsync(
        Guid inventoryItemId,
        ReorderRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .Include(i => i.Product)
                .ThenInclude(p => p.Supplier)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);

        if (item == null)
        {
            throw new KeyNotFoundException($"Inventory item with ID '{inventoryItemId}' was not found.");
        }

        var product = item.Product;
        var supplier = product.Supplier;

        return await _transactionService.ExecuteInTransactionAsync(async () =>
        {
            var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..18].ToUpper();
            var totalCost = product.CostPrice * request.Quantity;

            var po = new PurchaseOrder
            {
                SupplierId = supplier.Id,
                OrderNumber = orderNumber,
                TotalAmount = totalCost,
                Status = PurchaseOrderStatus.Submitted,
                OrderedAt = DateTime.UtcNow,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate.HasValue
                    ? DateTime.SpecifyKind(request.ExpectedDeliveryDate.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.PurchaseOrders.AddAsync(po, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var poItem = new PurchaseOrderItem
            {
                PurchaseOrderId = po.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
                UnitCost = product.CostPrice,
                TotalCost = totalCost
            };

            await _dbContext.PurchaseOrderItems.AddAsync(poItem, cancellationToken);

            await _dbContext.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "PurchaseOrder",
                EntityId = po.Id.ToString(),
                Action = "PURCHASE_ORDER_REORDER",
                UserId = userId,
                NewValuesJson = $"{{\"OrderNumber\":\"{orderNumber}\",\"ProductId\":\"{product.Id}\",\"Quantity\":{request.Quantity},\"TotalCost\":{totalCost}}}",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ReorderResponse
            {
                PurchaseOrderId = po.Id,
                OrderNumber = po.OrderNumber,
                ProductId = product.Id,
                ProductName = product.Name,
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                Quantity = request.Quantity,
                UnitCost = product.CostPrice,
                TotalCost = totalCost,
                Status = po.Status.ToString(),
                OrderedAt = po.OrderedAt
            };
        }, cancellationToken);
    }

    // ==========================================
    // CSV EXPORT / IMPORT
    // ==========================================

    public async Task<byte[]> ExportInventoryCsvAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.InventoryItems
            .AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Include(i => i.Product)
                .ThenInclude(p => p.Supplier)
            .OrderBy(i => i.Product.Name)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        // RFC 4180 CSV Header
        sb.AppendLine("SKU,ProductName,Category,Supplier,UnitPrice,CostPrice,QuantityInStock,ReorderThreshold,MaxStockLevel,LocationBin,IsLowStock");

        foreach (var item in items)
        {
            var sku = EscapeCsv(item.Product.SKU);
            var name = EscapeCsv(item.Product.Name);
            var category = EscapeCsv(item.Product.Category.Name);
            var supplier = EscapeCsv(item.Product.Supplier.Name);
            var unitPrice = item.Product.UnitPrice.ToString("F2", CultureInfo.InvariantCulture);
            var costPrice = item.Product.CostPrice.ToString("F2", CultureInfo.InvariantCulture);
            var qty = item.QuantityInStock.ToString(CultureInfo.InvariantCulture);
            var threshold = item.ReorderThreshold.ToString(CultureInfo.InvariantCulture);
            var maxStock = item.MaxStockLevel.ToString(CultureInfo.InvariantCulture);
            var bin = EscapeCsv(item.LocationBin ?? string.Empty);
            var isLowStock = item.QuantityInStock <= item.ReorderThreshold ? "TRUE" : "FALSE";

            sb.AppendLine($"{sku},{name},{category},{supplier},{unitPrice},{costPrice},{qty},{threshold},{maxStock},{bin},{isLowStock}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<CsvImportResult> ImportInventoryCsvAsync(Stream csvStream, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var result = new CsvImportResult();
        var rowNumber = 0;

        string? headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            result.Errors.Add(new CsvImportRowError { RowNumber = 0, SKU = string.Empty, Error = "CSV file is empty." });
            return result;
        }

        var defaultSupplier = await _dbContext.Suppliers.FirstOrDefaultAsync(cancellationToken);
        var defaultCategory = await _dbContext.ProductCategories.FirstOrDefaultAsync(cancellationToken);

        while (!reader.EndOfStream)
        {
            rowNumber++;
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.TotalRows++;
            var columns = ParseCsvLine(line);

            if (columns.Length < 6)
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError
                {
                    RowNumber = rowNumber,
                    SKU = columns.Length > 0 ? columns[0] : string.Empty,
                    Error = $"Invalid row column count: expected at least 6 columns, got {columns.Length}."
                });
                continue;
            }

            var sku = columns[0].Trim().ToUpperInvariant();
            var name = columns[1].Trim();
            var categoryName = columns[2].Trim();
            var supplierName = columns[3].Trim();
            var unitPriceStr = columns[4].Trim();
            var costPriceStr = columns[5].Trim();
            var qtyStr = columns.Length > 6 ? columns[6].Trim() : "0";
            var thresholdStr = columns.Length > 7 ? columns[7].Trim() : "10";
            var maxStockStr = columns.Length > 8 ? columns[8].Trim() : "100";
            var bin = columns.Length > 9 ? columns[9].Trim() : "Warehouse-A";

            if (string.IsNullOrWhiteSpace(sku))
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = "SKU cannot be empty." });
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = "Product Name cannot be empty." });
                continue;
            }

            if (!decimal.TryParse(unitPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice < 0)
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = $"Invalid UnitPrice '{unitPriceStr}' (must be >= 0)." });
                continue;
            }

            if (!decimal.TryParse(costPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var costPrice) || costPrice < 0)
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = $"Invalid CostPrice '{costPriceStr}' (must be >= 0)." });
                continue;
            }

            if (!int.TryParse(qtyStr, out var quantityInStock) || quantityInStock < 0)
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = $"Invalid QuantityInStock '{qtyStr}' (stock cannot be negative, must be >= 0)." });
                continue;
            }

            int.TryParse(thresholdStr, out var threshold);
            int.TryParse(maxStockStr, out var maxStock);
            if (maxStock < threshold) maxStock = threshold + 50;

            try
            {
                // Validate Category exists
                var category = await _dbContext.ProductCategories.FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower(), cancellationToken);
                if (category == null)
                {
                    result.FailureCount++;
                    result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = $"Product category '{categoryName}' does not exist." });
                    continue;
                }

                // Validate Supplier exists
                var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Name.ToLower() == supplierName.ToLower(), cancellationToken);
                if (supplier == null)
                {
                    result.FailureCount++;
                    result.Errors.Add(new CsvImportRowError { RowNumber = rowNumber, SKU = sku, Error = $"Supplier '{supplierName}' does not exist." });
                    continue;
                }

                // Upsert Product & InventoryItem
                var existingProduct = await _dbContext.Products
                    .Include(p => p.InventoryItem)
                    .FirstOrDefaultAsync(p => p.SKU.ToUpper() == sku, cancellationToken);

                if (existingProduct != null)
                {
                    existingProduct.Name = name;
                    existingProduct.CategoryId = category.Id;
                    existingProduct.SupplierId = supplier.Id;
                    existingProduct.UnitPrice = unitPrice;
                    existingProduct.CostPrice = costPrice;
                    existingProduct.UpdatedAt = DateTime.UtcNow;

                    if (existingProduct.InventoryItem != null)
                    {
                        var diff = quantityInStock - existingProduct.InventoryItem.QuantityInStock;
                        existingProduct.InventoryItem.QuantityInStock = quantityInStock;
                        existingProduct.InventoryItem.ReorderThreshold = threshold;
                        existingProduct.InventoryItem.MaxStockLevel = maxStock;
                        existingProduct.InventoryItem.LocationBin = bin;
                        existingProduct.InventoryItem.UpdatedAt = DateTime.UtcNow;

                        if (diff != 0)
                        {
                            var movement = new StockMovement
                            {
                                InventoryItemId = existingProduct.InventoryItem.Id,
                                QuantityChange = diff,
                                MovementType = diff > 0 ? StockMovementType.Restock : StockMovementType.Adjustment,
                                Reason = "CSV bulk import adjustment",
                                PerformedByUserId = userId,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
                        }
                    }
                }
                else
                {
                    var newProduct = new Product
                    {
                        SKU = sku,
                        Name = name,
                        CategoryId = category.Id,
                        SupplierId = supplier.Id,
                        UnitPrice = unitPrice,
                        CostPrice = costPrice,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Products.AddAsync(newProduct, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var newItem = new InventoryItem
                    {
                        ProductId = newProduct.Id,
                        QuantityInStock = quantityInStock,
                        ReorderThreshold = threshold,
                        MaxStockLevel = maxStock,
                        LocationBin = bin,
                        LastRestockedAt = quantityInStock > 0 ? DateTime.UtcNow : null,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _dbContext.InventoryItems.AddAsync(newItem, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    if (quantityInStock > 0)
                    {
                        var movement = new StockMovement
                        {
                            InventoryItemId = newItem.Id,
                            QuantityChange = quantityInStock,
                            MovementType = StockMovementType.Restock,
                            Reason = "Initial stock from CSV import",
                            PerformedByUserId = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add(new CsvImportRowError
                {
                    RowNumber = rowNumber,
                    SKU = sku,
                    Error = $"Row processing error: {ex.Message}"
                });
            }
        }

        return result;
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var sb = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }
}
