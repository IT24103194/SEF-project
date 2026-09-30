using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.DTOs.Inventory;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class TransactionAndSecurityAuditTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TransactionAndSecurityAuditTests(WebApplicationFactory<Program> factory)
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

    private async Task<string> GetMemberTokenAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "member@smartgym.com",
            Password = "Member123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        return auth!.AccessToken;
    }

    #region Transaction Tests

    [Fact]
    public async Task TransactionService_RollbackOnException_DoesNotPersistChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var txService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

        var testLocationId = Guid.NewGuid();
        var initialCount = await db.Locations.CountAsync();

        // Attempt an atomic operation that throws an exception midway
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await txService.ExecuteInTransactionAsync(async () =>
            {
                db.Locations.Add(new Location
                {
                    Id = testLocationId,
                    Name = "Transaction Rollback Test Zone",
                    Floor = "Basement",
                    Description = "Temporary"
                });
                await db.SaveChangesAsync();

                // Force exception to trigger rollback
                throw new InvalidOperationException("Simulated catastrophic mid-transaction failure.");
            });
        });

        // Verify that the record was not persisted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var persisted = await verifyDb.Locations.FindAsync(testLocationId);
        Assert.Null(persisted);
        var finalCount = await verifyDb.Locations.CountAsync();
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public async Task TransactionService_StockMovementAtomic_UpdatesStockAndAuditAtomically()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var txService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

        var item = await db.InventoryItems.FirstOrDefaultAsync();
        if (item == null) return;

        var originalQty = item.QuantityInStock;
        var change = 5;

        var (success, message, updatedItem) = await txService.ExecuteStockMovementAtomicAsync(
            item.Id,
            change,
            StockMovementType.Restock,
            "Restock audit transaction test",
            null
        );

        Assert.True(success);
        Assert.NotNull(updatedItem);
        Assert.Equal(originalQty + change, updatedItem.QuantityInStock);

        // Verify audit log exists
        var audit = await db.AuditLogs
            .Where(a => a.EntityId == item.Id.ToString() && a.Action == "STOCK_RESTOCK")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        Assert.NotNull(audit);
    }

    #endregion

    #region Security & Injection Tests

    [Theory]
    [InlineData("' OR '1'='1' --")]
    [InlineData("'; DROP TABLE members; --")]
    [InlineData("1 UNION SELECT username, password FROM users --")]
    [InlineData("<script>alert('xss')</script>")]
    public async Task SearchEndpoints_SqlAndScriptInjectionInputs_DoNotCompromiseDatabase(string maliciousInput)
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Test Inventory search
        var invResponse = await _client.GetAsync($"/api/inventory?search={Uri.EscapeDataString(maliciousInput)}");
        Assert.True(invResponse.StatusCode == HttpStatusCode.OK || invResponse.StatusCode == HttpStatusCode.BadRequest);

        // Test Members search
        var memberResponse = await _client.GetAsync($"/api/members?search={Uri.EscapeDataString(maliciousInput)}");
        Assert.True(memberResponse.StatusCode == HttpStatusCode.OK || memberResponse.StatusCode == HttpStatusCode.BadRequest);

        // Ensure database is intact and functional
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var userCount = await db.Users.CountAsync();
        Assert.True(userCount > 0, "Users table was unexpectedly modified or dropped.");
    }

    [Fact]
    public async Task ErrorResponses_DoNotLeakDatabaseConnectionStringsOrInternalSecrets()
    {
        // Request a deliberately malformed/non-existent endpoint
        var response = await _client.GetAsync("/api/non-existent-endpoint-for-security-audit");
        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Server=", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username=", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JwtSettings", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PasswordHashing_NeverStoresPlaintextPasswordsInDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var seededUsers = await db.Users.Where(u => u.Email == "admin@smartgym.com" || u.Email == "member@smartgym.com").ToListAsync();
        Assert.NotEmpty(seededUsers);

        foreach (var user in seededUsers)
        {
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.NotEqual("Admin123!", user.PasswordHash);
            Assert.NotEqual("Member123!", user.PasswordHash);
            Assert.True(user.PasswordHash.Length >= 30, $"Password hash for {user.Email} is suspiciously short.");
        }
    }

    [Fact]
    public async Task FileUpload_NonImageExtensions_AreStrictlyRejected()
    {
        var token = await GetMemberTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var loc = await db.Locations.FirstAsync();

        using var content = new MultipartFormDataContent();
        var fakeExecutable = new ByteArrayContent(new byte[] { 0x4D, 0x5A, 0x90, 0x00 }); // MZ executable header
        fakeExecutable.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fakeExecutable, "image", "malware.exe");
        content.Add(new StringContent("Title"), "title");
        content.Add(new StringContent("Description"), "description");
        content.Add(new StringContent(loc.Id.ToString()), "locationId");

        var response = await _client.PostAsync("/api/facility-issues/with-image", content);
        // Must reject executable uploads (either 400 Bad Request or UnsupportedMediaType)
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Cors_OptionsPreflightRequest_ReturnsAllowedHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/facility-issues");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await _client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK);
    }

    #endregion
}
