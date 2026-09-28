using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Inventory;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class InventoryModuleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InventoryModuleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@smartgym.com",
            Password = "Admin123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        return auth!.AccessToken;
    }

    private async Task<string> GetTrainerTokenAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "trainer@smartgym.com",
            Password = "Trainer123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        return auth!.AccessToken;
    }

    [Fact]
    public async Task Supplier_CompleteCrudLifecycle_Succeeds()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueName = $"Optimum Labs {Guid.NewGuid():N}";
        var createRequest = new CreateSupplierRequest
        {
            Name = uniqueName,
            ContactPerson = "John Smith",
            Email = $"orders_{Guid.NewGuid():N}@optimumlabs.com",
            Phone = "+1 800 555 0199",
            Address = "100 Protein Way, Muscle City, CA",
            IsActive = true
        };

        // 1. Create Supplier (POST /api/suppliers)
        var createResponse = await _client.PostAsJsonAsync("/api/suppliers", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(uniqueName, created.Name);

        // 2. Get Supplier by ID (GET /api/suppliers/{id})
        var getByIdResponse = await _client.GetAsync($"/api/suppliers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);
        var fetched = await getByIdResponse.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);

        // 3. Update Supplier (PUT /api/suppliers/{id})
        var updateRequest = new UpdateSupplierRequest
        {
            Name = $"{uniqueName} Updated",
            ContactPerson = "Jane Doe",
            Email = created.Email,
            Phone = "+1 800 555 9999",
            Address = "200 Whey Blvd, Muscle City, CA",
            IsActive = true
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/suppliers/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
        Assert.NotNull(updated);
        Assert.Equal($"{uniqueName} Updated", updated.Name);
        Assert.Equal("Jane Doe", updated.ContactPerson);

        // 4. Query Suppliers with Search (GET /api/suppliers?searchTerm=...)
        var searchResponse = await _client.GetAsync($"/api/suppliers?searchTerm={Uri.EscapeDataString(uniqueName)}");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<PagedResult<SupplierDto>>(_jsonOptions);
        Assert.NotNull(searchResult);
        Assert.NotEmpty(searchResult.Items);
        Assert.Contains(searchResult.Items, s => s.Id == created.Id);

        // 5. Delete Supplier (DELETE /api/suppliers/{id})
        var deleteResponse = await _client.DeleteAsync($"/api/suppliers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it is gone
        var verifyDeletedResponse = await _client.GetAsync($"/api/suppliers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, verifyDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task ProductAndInventory_CreateAdjustReorder_EnforcesBusinessRules()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // First, ensure a category and supplier exist
        var supplierResponse = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            Name = $"Pure Nutrition {Guid.NewGuid():N}",
            ContactPerson = "Alex Stone",
            Email = $"alex_{Guid.NewGuid():N}@purenutrition.com",
            Phone = "+1 555 0144"
        });
        supplierResponse.EnsureSuccessStatusCode();
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);

        var categoriesResponse = await _client.GetAsync("/api/products/categories");
        categoriesResponse.EnsureSuccessStatusCode();
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<ProductCategoryDto>>(_jsonOptions);
        var categoryId = categories!.First().Id;

        // 1. Create Product with initial stock 15
        var sku = $"WHEY-{Guid.NewGuid():N}"[..12].ToUpper();
        var createProductRequest = new CreateProductRequest
        {
            CategoryId = categoryId,
            SupplierId = supplier!.Id,
            SKU = sku,
            Name = "Whey Isolate Protein 2kg",
            Description = "Premium cross-flow microfiltered whey isolate",
            UnitPrice = 89.99m,
            CostPrice = 45.00m,
            InitialStock = 15,
            ReorderThreshold = 10,
            MaxStockLevel = 100,
            LocationBin = "Bin-A12",
            IsActive = true
        };

        var productResponse = await _client.PostAsJsonAsync("/api/products", createProductRequest);
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        Assert.NotNull(product);
        Assert.NotNull(product.InventoryItemId);
        Assert.Equal(15, product.QuantityInStock);
        Assert.False(product.IsLowStock); // 15 > 10

        var inventoryItemId = product.InventoryItemId.Value;

        // 2. Adjust stock downward to trigger low stock (15 - 8 = 7, <= threshold 10)
        var adjustDownResponse = await _client.PostAsJsonAsync($"/api/inventory/{inventoryItemId}/adjust", new StockAdjustmentRequest
        {
            QuantityChange = -8,
            MovementType = StockMovementType.Sale,
            Reason = "In-gym shake counter sale"
        });
        Assert.Equal(HttpStatusCode.OK, adjustDownResponse.StatusCode);
        var itemAfterSale = await adjustDownResponse.Content.ReadFromJsonAsync<InventoryItemDto>(_jsonOptions);
        Assert.NotNull(itemAfterSale);
        Assert.Equal(7, itemAfterSale.QuantityInStock);
        Assert.True(itemAfterSale.IsLowStock);

        // 3. Business rule: Stock cannot become negative! (Attempt to deduct 10 when stock is 7)
        var negativeStockResponse = await _client.PostAsJsonAsync($"/api/inventory/{inventoryItemId}/adjust", new StockAdjustmentRequest
        {
            QuantityChange = -10,
            MovementType = StockMovementType.Adjustment,
            Reason = "Invalid excess deduction"
        });
        Assert.Equal(HttpStatusCode.BadRequest, negativeStockResponse.StatusCode);
        var negError = await negativeStockResponse.Content.ReadAsStringAsync();
        Assert.Contains("negative", negError, StringComparison.OrdinalIgnoreCase);

        // Verify stock is still 7
        var getInventoryResponse = await _client.GetAsync($"/api/inventory/{inventoryItemId}");
        var currentItem = await getInventoryResponse.Content.ReadFromJsonAsync<InventoryItemDto>(_jsonOptions);
        Assert.Equal(7, currentItem!.QuantityInStock);

        // 4. Verify Stock Movement History was recorded
        var historyResponse = await _client.GetAsync($"/api/inventory/{inventoryItemId}/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var movements = await historyResponse.Content.ReadFromJsonAsync<List<StockMovementDto>>(_jsonOptions);
        Assert.NotNull(movements);
        Assert.True(movements.Count >= 2); // Initial stock (+15) and Sale (-8)
        Assert.Contains(movements, m => m.QuantityChange == 15 && m.MovementType == "Restock");
        Assert.Contains(movements, m => m.QuantityChange == -8 && m.MovementType == "Sale");

        // 5. Perform Reorder (POST /api/inventory/{id}/reorder)
        var reorderResponse = await _client.PostAsJsonAsync($"/api/inventory/{inventoryItemId}/reorder", new ReorderRequest
        {
            Quantity = 25,
            Notes = "Urgent replenishment for supplement store"
        });
        Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);
        var reorder = await reorderResponse.Content.ReadFromJsonAsync<ReorderResponse>(_jsonOptions);
        Assert.NotNull(reorder);
        Assert.Equal(product.Id, reorder.ProductId);
        Assert.Equal(supplier.Id, reorder.SupplierId);
        Assert.Equal(25, reorder.Quantity);
        Assert.Equal(45.00m, reorder.UnitCost);
        Assert.Equal(25 * 45.00m, reorder.TotalCost);
        Assert.Equal("Submitted", reorder.Status);
    }

    [Fact]
    public async Task Inventory_LowStockFiltering_CorrectlyFiltersItems()
    {
        // Arrange
        var token = await GetTrainerTokenAsync(); // Trainer / Staff role
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Query low stock items
        var response = await _client.GetAsync("/api/inventory?isLowStock=true&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<InventoryItemDto>>(_jsonOptions);
        Assert.NotNull(paged);

        foreach (var item in paged.Items)
        {
            Assert.True(item.IsLowStock);
            Assert.True(item.QuantityInStock <= item.ReorderThreshold);
        }
    }

    [Fact]
    public async Task CsvExportAndImport_ExecutesWithRowLevelValidation()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Test CSV Export (GET /api/inventory/export-csv)
        var exportResponse = await _client.GetAsync("/api/inventory/export-csv");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType?.MediaType);

        var csvContent = await exportResponse.Content.ReadAsStringAsync();
        Assert.NotEmpty(csvContent);
        var firstLine = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
        Assert.Contains("SKU", firstLine);
        Assert.Contains("ProductName", firstLine);
        Assert.Contains("UnitPrice", firstLine);

        // 2. Prepare CSV for Import with 1 valid row and 2 invalid rows (one with negative stock, one with missing category)
        var validSku = $"IMP-{Guid.NewGuid():N}"[..10].ToUpper();
        var invalidSku = $"BAD-{Guid.NewGuid():N}"[..10].ToUpper();

        // Get an existing supplier and category
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var supplier = db.Suppliers.First();
        var category = db.ProductCategories.First();
        var nonExistentCategory = $"MissingCat_{Guid.NewGuid():N}";

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("SKU,ProductName,CategoryName,SupplierName,UnitPrice,CostPrice,InitialStock,ReorderThreshold,MaxStockLevel,LocationBin,Description");
        // Valid row
        csvBuilder.AppendLine($"{validSku},Imported BCAA 500g,{category.Name},{supplier.Name},39.99,19.99,50,10,200,Warehouse-B,Clean BCAA Formula");
        // Invalid row 1: Negative stock
        csvBuilder.AppendLine($"{invalidSku},Invalid Negative Stock Item,{category.Name},{supplier.Name},29.99,14.99,-5,10,100,Warehouse-B,Test");
        // Invalid row 2: Non-existent category
        csvBuilder.AppendLine($"NOCAT-{Guid.NewGuid():N},Missing Cat Item,{nonExistentCategory},{supplier.Name},29.99,14.99,10,5,100,Warehouse-B,Test");

        using var multiContent = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var byteContent = new ByteArrayContent(fileBytes);
        byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        multiContent.Add(byteContent, "file", "test_import.csv");

        // 3. Test CSV Import (POST /api/inventory/import-csv)
        var importResponse = await _client.PostAsync("/api/inventory/import-csv", multiContent);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        var importResult = await importResponse.Content.ReadFromJsonAsync<CsvImportResult>(_jsonOptions);
        Assert.NotNull(importResult);
        Assert.Equal(3, importResult.TotalRows);
        Assert.Equal(1, importResult.SuccessCount);
        Assert.Equal(2, importResult.FailureCount);
        Assert.NotEmpty(importResult.Errors);
        Assert.Contains(importResult.Errors, e => e.SKU == invalidSku && e.Error.Contains("negative", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(importResult.Errors, e => e.Error.Contains(nonExistentCategory));

        // Verify the valid imported product actually exists in the database
        var verifyProductResponse = await _client.GetAsync($"/api/products?searchTerm={validSku}");
        Assert.Equal(HttpStatusCode.OK, verifyProductResponse.StatusCode);
        var verifyResult = await verifyProductResponse.Content.ReadFromJsonAsync<PagedResult<ProductDto>>(_jsonOptions);
        Assert.NotNull(verifyResult);
        Assert.Single(verifyResult.Items);
        Assert.Equal(50, verifyResult.Items[0].QuantityInStock);
    }
}
