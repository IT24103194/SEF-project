using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Data;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class DatabaseModelTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseModelTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private SmartGymDbContext CreateDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
    }

    [Fact]
    public async Task SeedExecution_PopulatesCoreEntitiesSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        // Act
        await seeder.SeedAsync();

        // Assert - Roles
        var roles = await db.Roles.ToListAsync();
        Assert.Contains(roles, r => r.Name == "Admin");
        Assert.Contains(roles, r => r.Name == "Trainer");
        Assert.Contains(roles, r => r.Name == "Member");

        // Assert - Users
        var admin = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == "admin@smartgym.com");
        Assert.NotNull(admin);
        Assert.True(BCrypt.Net.BCrypt.Verify("Admin123!", admin.PasswordHash));
        Assert.Contains(admin.UserRoles, ur => ur.Role.Name == "Admin");

        var trainer = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == "trainer@smartgym.com");
        Assert.NotNull(trainer);
        Assert.True(BCrypt.Net.BCrypt.Verify("Trainer123!", trainer.PasswordHash));

        var memberUser = await db.Users.Include(u => u.MemberProfile).FirstOrDefaultAsync(u => u.Email == "member@smartgym.com");
        Assert.NotNull(memberUser);
        Assert.NotNull(memberUser.MemberProfile);
        Assert.Equal("Anusha Perera", memberUser.MemberProfile.EmergencyContactName);

        // Assert - Membership Plans
        var plans = await db.MembershipPlans.ToListAsync();
        Assert.True(plans.Count >= 4);
        Assert.Contains(plans, p => p.Name == "Gold Tier" && p.Price == 32000.00m);

        // Assert - Active Membership & Goals
        var activeMembership = await db.Memberships.FirstOrDefaultAsync(m => m.MemberId == memberUser.MemberProfile.Id && m.Status == MembershipStatus.Active);
        Assert.NotNull(activeMembership);

        var goals = await db.Goals.Include(g => g.ProgressRecords).Where(g => g.MemberId == memberUser.MemberProfile.Id).ToListAsync();
        Assert.NotEmpty(goals);

        // Assert - Locations & Equipment
        var locations = await db.Locations.ToListAsync();
        Assert.Contains(locations, l => l.Name == "Cardio Zone A");
        Assert.Contains(locations, l => l.Name == "Free Weights Area");

        var equipment = await db.Equipment.ToListAsync();
        Assert.Contains(equipment, e => e.SerialNumber == "LF-TRD-2023-001");
        Assert.Contains(equipment, e => e.SerialNumber == "TG-SKM-2024-003");

        // Assert - Maintenance & AI Workflow
        var issues = await db.FacilityIssues.Include(fi => fi.AIWorkflow).ThenInclude(w => w!.Steps).ToListAsync();
        Assert.NotEmpty(issues);
        var issueWithAi = issues.FirstOrDefault(i => i.AIWorkflow != null && i.AIWorkflow.Steps.Any()) 
            ?? issues.FirstOrDefault(i => i.AIWorkflow != null);
        Assert.NotNull(issueWithAi);
        Assert.NotNull(issueWithAi.AIWorkflow);
        Assert.Equal("gemini-1.5-pro", issueWithAi.AIWorkflow.ModelIdentifier);
        Assert.NotEmpty(issueWithAi.AIWorkflow.Steps);

        // Assert - Classes & Schedules
        var classes = await db.FitnessClasses.ToListAsync();
        Assert.Contains(classes, c => c.Name == "Metabolic Blast HIIT");

        var bookings = await db.Bookings.ToListAsync();
        Assert.NotEmpty(bookings);

        // Assert - Suppliers, Products & Inventory
        var suppliers = await db.Suppliers.ToListAsync();
        Assert.Contains(suppliers, s => s.Name == "NutriFit Lanka Pvt Ltd");

        var products = await db.Products.Include(p => p.InventoryItem).ToListAsync();
        var whey = products.FirstOrDefault(p => p.SKU == "ON-WHEY-5LB-CHOC");
        Assert.NotNull(whey);
        Assert.NotNull(whey.InventoryItem);
        Assert.True(whey.InventoryItem.QuantityInStock > 0);
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateUserEmail_ThrowsDbUpdateException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var uniqueEmail = $"dupetest_{Guid.NewGuid():N}@smartgym.com";

        var user1 = new User
        {
            Email = uniqueEmail,
            FirstName = "Test1",
            LastName = "User1",
            PasswordHash = "hashed_pw",
            CreatedAt = DateTime.UtcNow
        };
        await db.Users.AddAsync(user1);
        await db.SaveChangesAsync();

        var user2 = new User
        {
            Email = uniqueEmail, // Duplicate!
            FirstName = "Test2",
            LastName = "User2",
            PasswordHash = "hashed_pw_2",
            CreatedAt = DateTime.UtcNow
        };
        await db.Users.AddAsync(user2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateProductSKU_ThrowsDbUpdateException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var supplier = await db.Suppliers.FirstAsync();
        var category = await db.ProductCategories.FirstAsync();
        var duplicateSku = $"SKU-TEST-{Guid.NewGuid():N}"[..20];

        var prod1 = new Product
        {
            SupplierId = supplier.Id,
            CategoryId = category.Id,
            SKU = duplicateSku,
            Name = "Product 1",
            UnitPrice = 1000m,
            CostPrice = 700m,
            CreatedAt = DateTime.UtcNow
        };
        await db.Products.AddAsync(prod1);
        await db.SaveChangesAsync();

        var prod2 = new Product
        {
            SupplierId = supplier.Id,
            CategoryId = category.Id,
            SKU = duplicateSku, // Duplicate SKU!
            Name = "Product 2",
            UnitPrice = 1200m,
            CostPrice = 800m,
            CreatedAt = DateTime.UtcNow
        };
        await db.Products.AddAsync(prod2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateEquipmentSerialNumber_ThrowsDbUpdateException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var location = await db.Locations.FirstAsync();
        var duplicateSerial = $"SN-TEST-{Guid.NewGuid():N}"[..20];

        var eq1 = new Equipment
        {
            LocationId = location.Id,
            SerialNumber = duplicateSerial,
            Name = "Treadmill Alpha",
            Model = "Mod-A",
            Manufacturer = "TestMfg",
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await db.Equipment.AddAsync(eq1);
        await db.SaveChangesAsync();

        var eq2 = new Equipment
        {
            LocationId = location.Id,
            SerialNumber = duplicateSerial, // Duplicate S/N!
            Name = "Treadmill Beta",
            Model = "Mod-B",
            Manufacturer = "TestMfg",
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await db.Equipment.AddAsync(eq2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ForeignKeyConstraint_InvalidLocationId_ThrowsDbUpdateException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var invalidLocationId = Guid.NewGuid(); // Non-existent foreign key!
        var equipment = new Equipment
        {
            LocationId = invalidLocationId,
            SerialNumber = $"SN-FK-TEST-{Guid.NewGuid():N}"[..20],
            Name = "Orphan Equipment",
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await db.Equipment.AddAsync(equipment);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task CheckConstraint_NegativePrice_ThrowsDbUpdateException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var supplier = await db.Suppliers.FirstAsync();
        var category = await db.ProductCategories.FirstAsync();

        var invalidProduct = new Product
        {
            SupplierId = supplier.Id,
            CategoryId = category.Id,
            SKU = $"NEG-PRICE-{Guid.NewGuid():N}"[..20],
            Name = "Invalid Negative Price Item",
            UnitPrice = -500.00m, // Violates CK_products_unit_price!
            CostPrice = 200.00m,
            CreatedAt = DateTime.UtcNow
        };
        await db.Products.AddAsync(invalidProduct);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task TransactionService_ExecuteStockMovementAtomic_SucceedsAndLogsAudit()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var txService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var item = await db.InventoryItems.Include(i => i.Product).FirstAsync();
        var initialStock = item.QuantityInStock;
        var adjustment = 5;

        // Act
        var result = await txService.ExecuteStockMovementAtomicAsync(
            item.Id,
            adjustment,
            StockMovementType.Adjustment,
            "Cycle count adjustment verified",
            null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal(initialStock + adjustment, result.Item.QuantityInStock);

        // Verify audit log entry
        var audit = await db.AuditLogs
            .Where(a => a.EntityName == "InventoryItem" && a.EntityId == item.Id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(audit);
        Assert.Contains("STOCK_ADJUSTMENT", audit.Action);
    }

    [Fact]
    public async Task TransactionService_RollbackOnFailure_LeavesDatabaseUnchanged()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var txService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var testCategoryName = $"Category-TxRollback-{Guid.NewGuid():N}"[..25];

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await txService.ExecuteInTransactionAsync(async () =>
            {
                var newCat = new ProductCategory
                {
                    Name = testCategoryName,
                    Description = "Temporary category that will roll back",
                    CreatedAt = DateTime.UtcNow
                };
                await db.ProductCategories.AddAsync(newCat);
                await db.SaveChangesAsync();

                // Force a deliberate failure inside transaction
                throw new InvalidOperationException("Simulated business validation error triggering rollback.");
            });
        });

        // Verify that the record does NOT exist in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var rolledBackCat = await verifyDb.ProductCategories.FirstOrDefaultAsync(c => c.Name == testCategoryName);
        Assert.Null(rolledBackCat);
    }
}
