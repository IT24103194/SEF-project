using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Authorization;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Notifications;
using SmartGym.Api.DTOs.Reporting;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class ReportingAndNotificationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReportingAndNotificationTests(WebApplicationFactory<Program> factory)
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

    private async Task<(User User, string Token)> CreateIsolatedUserAsync(string role = AppRoles.Member)
    {
        var email = $"user_{Guid.NewGuid():N}@smartgym.com";
        var password = "Password123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var user = new User
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+94770000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dbRole = await db.Roles.FirstAsync(r => r.Name == role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = dbRole.Id });
        await db.SaveChangesAsync();

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginRes.EnsureSuccessStatusCode();
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);

        return (user, auth!.AccessToken);
    }

    [Fact]
    public async Task Staff_Can_Access_All_Report_Endpoints()
    {
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Membership Report
        var memRes = await _client.GetAsync("/api/reports/membership");
        Assert.Equal(HttpStatusCode.OK, memRes.StatusCode);
        var memReport = await memRes.Content.ReadFromJsonAsync<MembershipReportDto>(_jsonOptions);
        Assert.NotNull(memReport);
        Assert.True(memReport.TotalMembers >= 0);

        // 2. Classes Report
        var clsRes = await _client.GetAsync("/api/reports/classes");
        Assert.Equal(HttpStatusCode.OK, clsRes.StatusCode);
        var clsReport = await clsRes.Content.ReadFromJsonAsync<ClassesReportDto>(_jsonOptions);
        Assert.NotNull(clsReport);

        // 3. Inventory Report
        var invRes = await _client.GetAsync("/api/reports/inventory");
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);
        var invReport = await invRes.Content.ReadFromJsonAsync<InventoryReportDto>(_jsonOptions);
        Assert.NotNull(invReport);

        // 4. Facility Report
        var facRes = await _client.GetAsync("/api/reports/facility");
        Assert.Equal(HttpStatusCode.OK, facRes.StatusCode);
        var facReport = await facRes.Content.ReadFromJsonAsync<FacilityReportDto>(_jsonOptions);
        Assert.NotNull(facReport);

        // 5. AI Report
        var aiRes = await _client.GetAsync("/api/reports/ai");
        Assert.Equal(HttpStatusCode.OK, aiRes.StatusCode);
        var aiReport = await aiRes.Content.ReadFromJsonAsync<AiReportDto>(_jsonOptions);
        Assert.NotNull(aiReport);

        // 6. Executive Dashboard
        var dashRes = await _client.GetAsync("/api/reports/executive-dashboard");
        Assert.Equal(HttpStatusCode.OK, dashRes.StatusCode);
        var dash = await dashRes.Content.ReadFromJsonAsync<ExecutiveDashboardDto>(_jsonOptions);
        Assert.NotNull(dash);
        Assert.NotNull(dash.Membership);
        Assert.NotNull(dash.Classes);
        Assert.NotNull(dash.Inventory);
        Assert.NotNull(dash.Facility);
        Assert.NotNull(dash.Ai);
    }

    [Fact]
    public async Task Member_Cannot_Access_Reports_Forbidden()
    {
        var (_, memberToken) = await CreateIsolatedUserAsync(AppRoles.Member);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var res = await _client.GetAsync("/api/reports/executive-dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Notification_Lifecycle_Creation_Event_Trigger_Read_And_Unread_Count()
    {
        var (user, userToken) = await CreateIsolatedUserAsync(AppRoles.Member);
        var adminToken = await GetAdminTokenAsync();

        // 1. Trigger Booking Confirmation event for user
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var eventRes = await _client.PostAsJsonAsync("/api/notifications/events/trigger", new TriggerEventNotificationRequest
        {
            EventType = NotificationEventType.BookingConfirmation,
            UserId = user.Id,
            ReferenceName = "Metabolic Blast HIIT",
            Details = "Studio 1"
        });
        Assert.Equal(HttpStatusCode.OK, eventRes.StatusCode);
        var notif1 = await eventRes.Content.ReadFromJsonAsync<NotificationDto>(_jsonOptions);
        Assert.NotNull(notif1);
        Assert.False(notif1.IsRead);
        Assert.Equal(NotificationType.Booking, notif1.Type);

        // 2. Trigger Membership Expiry event for user
        var event2Res = await _client.PostAsJsonAsync("/api/notifications/events/trigger", new TriggerEventNotificationRequest
        {
            EventType = NotificationEventType.MembershipExpiry,
            UserId = user.Id,
            ReferenceName = "Gold Plan",
            ScheduledDate = DateTime.UtcNow.AddDays(5)
        });
        Assert.Equal(HttpStatusCode.OK, event2Res.StatusCode);

        // 3. User checks unread count -> should be at least 2
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var unreadRes = await _client.GetAsync("/api/notifications/unread-count");
        Assert.Equal(HttpStatusCode.OK, unreadRes.StatusCode);
        var countObj = await unreadRes.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(countObj.GetProperty("unreadCount").GetInt32() >= 2);

        // 4. User queries their notifications
        var listRes = await _client.GetAsync("/api/notifications?unreadOnly=true");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var pagedNotifs = await listRes.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>(_jsonOptions);
        Assert.NotNull(pagedNotifs);
        Assert.True(pagedNotifs.Items.Count >= 2);

        // 5. Mark first notification as read
        var readRes = await _client.PutAsync($"/api/notifications/{notif1.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, readRes.StatusCode);
        var readNotif = await readRes.Content.ReadFromJsonAsync<NotificationDto>(_jsonOptions);
        Assert.True(readNotif!.IsRead);

        // 6. Mark all as read
        var markAllRes = await _client.PutAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, markAllRes.StatusCode);

        // 7. Verify unread count is now 0
        var zeroCountRes = await _client.GetAsync("/api/notifications/unread-count");
        var zeroCountObj = await zeroCountRes.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal(0, zeroCountObj.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Reusable_Pagination_Parameters_And_Queryable_Extensions_Work()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var pagination = new PaginationParameters(1, 5);
        var pagedUsers = await db.Users.OrderBy(u => u.Email).ToPagedResultAsync(pagination);

        Assert.NotNull(pagedUsers);
        Assert.Equal(1, pagedUsers.PageNumber);
        Assert.Equal(5, pagedUsers.PageSize);
        Assert.True(pagedUsers.TotalCount >= 0);
        Assert.True(pagedUsers.Items.Count <= 5);
    }
}
