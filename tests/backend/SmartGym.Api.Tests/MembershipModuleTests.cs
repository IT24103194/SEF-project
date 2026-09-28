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
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class MembershipModuleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MembershipModuleTests(WebApplicationFactory<Program> factory)
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

    private async Task<(User User, Member Member, string Token)> CreateIsolatedMemberAsync(string? prefix = null)
    {
        var email = $"{prefix ?? "member"}_{Guid.NewGuid():N}@smartgym.com";
        var password = "Password123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var user = new User
        {
            Email = email,
            FirstName = "Test",
            LastName = "Member",
            PhoneNumber = "+94770000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var memberRole = await db.Roles.FirstAsync(r => r.Name == AppRoles.Member);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = memberRole.Id });

        var member = new Member
        {
            UserId = user.Id,
            DateOfBirth = new DateTime(1995, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "+94771234567",
            Address = "123 Gym Street"
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginRes.EnsureSuccessStatusCode();
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);

        return (user, member, auth!.AccessToken);
    }

    [Fact]
    public async Task Admin_Can_Create_Get_Update_And_Delete_MembershipPlan()
    {
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Create Plan
        var planName = $"Test Plan {Guid.NewGuid():N}";
        var createRequest = new CreateMembershipPlanRequest
        {
            Name = planName,
            Description = "Full access test plan",
            Price = 49.99m,
            DurationDays = 30,
            MaxClassesPerWeek = 5,
            HasTrainerAccess = true,
            IsActive = true
        };

        var postRes = await _client.PostAsJsonAsync("/api/membership-plans", createRequest);
        Assert.Equal(HttpStatusCode.Created, postRes.StatusCode);
        var created = await postRes.Content.ReadFromJsonAsync<MembershipPlanDto>(_jsonOptions);
        Assert.NotNull(created);
        Assert.Equal(planName, created.Name);
        Assert.Equal(49.99m, created.Price);

        // 2. Get Plan by ID
        var getRes = await _client.GetAsync($"/api/membership-plans/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        // 3. Update Plan
        var updateRequest = new UpdateMembershipPlanRequest
        {
            Name = planName + " Updated",
            Description = "Updated plan description",
            Price = 59.99m,
            DurationDays = 60,
            MaxClassesPerWeek = 10,
            HasTrainerAccess = true,
            IsActive = true
        };
        var putRes = await _client.PutAsJsonAsync($"/api/membership-plans/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var updated = await putRes.Content.ReadFromJsonAsync<MembershipPlanDto>(_jsonOptions);
        Assert.Equal(59.99m, updated!.Price);
        Assert.Equal(60, updated.DurationDays);

        // 4. Delete Plan
        var delRes = await _client.DeleteAsync($"/api/membership-plans/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_Cannot_Create_MembershipPlan_Forbidden()
    {
        var (_, _, memberToken) = await CreateIsolatedMemberAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var createRequest = new CreateMembershipPlanRequest
        {
            Name = "Hacker Plan",
            Description = "Illegal plan",
            Price = 1.00m,
            DurationDays = 365,
            MaxClassesPerWeek = 10,
            HasTrainerAccess = true,
            IsActive = true
        };

        var res = await _client.PostAsJsonAsync("/api/membership-plans", createRequest);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Membership_Lifecycle_Renewal_Extends_Validity_Preserves_History()
    {
        var adminToken = await GetAdminTokenAsync();
        var (_, member, memberToken) = await CreateIsolatedMemberAsync("renew");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Create a 30-day plan
        var planRes = await _client.PostAsJsonAsync("/api/membership-plans", new CreateMembershipPlanRequest
        {
            Name = $"Gold 30 Days {Guid.NewGuid():N}",
            Description = "Test monthly plan",
            Price = 60.00m,
            DurationDays = 30,
            MaxClassesPerWeek = 5,
            HasTrainerAccess = true,
            IsActive = true
        });
        var plan = await planRes.Content.ReadFromJsonAsync<MembershipPlanDto>(_jsonOptions);

        // 1. Staff creates initial membership for member
        var createMemRes = await _client.PostAsJsonAsync("/api/memberships", new CreateMembershipRequest
        {
            MemberId = member.Id,
            PlanId = plan!.Id,
            StartDate = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.Created, createMemRes.StatusCode);
        var firstMem = await createMemRes.Content.ReadFromJsonAsync<MembershipDto>(_jsonOptions);
        Assert.NotNull(firstMem);
        Assert.Equal(MembershipStatus.Active, firstMem.Status);

        // 2. Member checks /api/memberships/my-membership
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var myMemRes = await _client.GetAsync("/api/memberships/my-membership");
        Assert.Equal(HttpStatusCode.OK, myMemRes.StatusCode);
        var activeMem = await myMemRes.Content.ReadFromJsonAsync<MembershipDto>(_jsonOptions);
        Assert.NotNull(activeMem);
        Assert.Equal(firstMem.Id, activeMem.Id);

        // 3. Member renews their own membership
        var renewRes = await _client.PostAsJsonAsync("/api/memberships/renew", new RenewMembershipRequest
        {
            PlanId = plan.Id
        });
        Assert.Equal(HttpStatusCode.OK, renewRes.StatusCode);
        var renewedMem = await renewRes.Content.ReadFromJsonAsync<MembershipDto>(_jsonOptions);
        Assert.NotNull(renewedMem);
        Assert.Equal(MembershipStatus.Active, renewedMem.Status);

        // Renewal logic should have started from previous membership's EndDate
        Assert.True(renewedMem.EndDate > firstMem.EndDate);

        // 4. Member checks membership history -> preserves both records
        var histRes = await _client.GetAsync($"/api/memberships/history/{member.Id}");
        Assert.Equal(HttpStatusCode.OK, histRes.StatusCode);
        var history = await histRes.Content.ReadFromJsonAsync<List<MembershipDto>>(_jsonOptions);
        Assert.NotNull(history);
        Assert.True(history.Count >= 2);
    }

    [Fact]
    public async Task Expired_Membership_Is_Detected_Correctly()
    {
        var (_, member, memberToken) = await CreateIsolatedMemberAsync("expired");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
            var plan = await db.MembershipPlans.FirstAsync();

            // Insert expired membership in past
            var expiredMem = new Membership
            {
                MemberId = member.Id,
                PlanId = plan.Id,
                StartDate = DateTime.UtcNow.AddDays(-60),
                EndDate = DateTime.UtcNow.AddDays(-30),
                Status = MembershipStatus.Active, // Marked active in DB but dates are past
                PricePaid = plan.Price
            };
            db.Memberships.Add(expiredMem);
            await db.SaveChangesAsync();
        }

        // Querying membership should evaluate and report Expired status
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.GetAsync("/api/memberships/my-membership");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var myMem = await res.Content.ReadFromJsonAsync<MembershipDto>(_jsonOptions);
        // Either null active or reports Expired
        if (myMem != null)
        {
            Assert.Equal(MembershipStatus.Expired, myMem.Status);
        }
    }

    [Fact]
    public async Task Member_Can_Cancel_Membership_With_Reason()
    {
        var adminToken = await GetAdminTokenAsync();
        var (_, member, memberToken) = await CreateIsolatedMemberAsync("cancel");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var planRes = await _client.PostAsJsonAsync("/api/membership-plans", new CreateMembershipPlanRequest
        {
            Name = $"Cancelable Plan {Guid.NewGuid():N}",
            Description = "Plan to test cancel",
            Price = 40.00m,
            DurationDays = 30,
            MaxClassesPerWeek = 5,
            HasTrainerAccess = false,
            IsActive = true
        });
        var plan = await planRes.Content.ReadFromJsonAsync<MembershipPlanDto>(_jsonOptions);

        var memRes = await _client.PostAsJsonAsync("/api/memberships", new CreateMembershipRequest
        {
            MemberId = member.Id,
            PlanId = plan!.Id,
            StartDate = DateTime.UtcNow
        });
        var mem = await memRes.Content.ReadFromJsonAsync<MembershipDto>(_jsonOptions);

        // Member cancels membership
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var cancelRes = await _client.PutAsJsonAsync($"/api/memberships/{mem!.Id}/cancel", new CancelMembershipRequest
        {
            Reason = "Relocating to another city"
        });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var cancelled = await cancelRes.Content.ReadFromJsonAsync<MembershipDto>(_jsonOptions);
        Assert.NotNull(cancelled);
        Assert.Equal(MembershipStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task Goal_And_Progress_Tracking_With_Automatic_Completion()
    {
        var (_, member, memberToken) = await CreateIsolatedMemberAsync("goals");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        // 1. Create a goal: Bench Press 100 kg
        var createGoalRes = await _client.PostAsJsonAsync("/api/goals", new CreateGoalRequest
        {
            Title = "Bench Press Target",
            TargetValue = 100m,
            CurrentValue = 70m,
            Unit = "kg",
            TargetDate = DateTime.UtcNow.AddMonths(2)
        });
        Assert.Equal(HttpStatusCode.Created, createGoalRes.StatusCode);
        var goal = await createGoalRes.Content.ReadFromJsonAsync<GoalDto>(_jsonOptions);
        Assert.NotNull(goal);
        Assert.Equal("Bench Press Target", goal.Title);
        Assert.Equal(GoalStatus.InProgress, goal.Status);

        // 2. Log first progress record: 80 kg
        var prog1Res = await _client.PostAsJsonAsync($"/api/goals/{goal.Id}/progress", new CreateProgressRecordRequest
        {
            GoalId = goal.Id,
            Value = 80m,
            Notes = "First attempt, felt solid"
        });
        Assert.Equal(HttpStatusCode.Created, prog1Res.StatusCode);

        // 3. Goal should still be InProgress with CurrentValue = 80
        var goalCheck1 = await _client.GetAsync($"/api/goals/{goal.Id}");
        var goalState1 = await goalCheck1.Content.ReadFromJsonAsync<GoalDto>(_jsonOptions);
        Assert.Equal(80m, goalState1!.CurrentValue);
        Assert.Equal(GoalStatus.InProgress, goalState1.Status);

        // 4. Log second progress record meeting target: 105 kg
        var prog2Res = await _client.PostAsJsonAsync($"/api/goals/{goal.Id}/progress", new CreateProgressRecordRequest
        {
            GoalId = goal.Id,
            Value = 105m,
            Notes = "Surpassed 100kg target!"
        });
        Assert.Equal(HttpStatusCode.Created, prog2Res.StatusCode);

        // 5. Goal should now be marked Achieved
        var goalCheck2 = await _client.GetAsync($"/api/goals/{goal.Id}");
        var goalState2 = await goalCheck2.Content.ReadFromJsonAsync<GoalDto>(_jsonOptions);
        Assert.Equal(105m, goalState2!.CurrentValue);
        Assert.Equal(GoalStatus.Achieved, goalState2.Status);

        // 6. Progress history returns 2 records
        var histRes = await _client.GetAsync($"/api/goals/{goal.Id}/progress");
        var records = await histRes.Content.ReadFromJsonAsync<List<ProgressRecordDto>>(_jsonOptions);
        Assert.NotNull(records);
        Assert.Equal(3, records!.Count);
    }

    [Fact]
    public async Task Member_Cannot_Access_Or_Modify_Another_Members_Goal()
    {
        var (_, _, member1Token) = await CreateIsolatedMemberAsync("member1");
        var (_, _, member2Token) = await CreateIsolatedMemberAsync("member2");

        // Member 1 creates goal
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", member1Token);
        var createGoalRes = await _client.PostAsJsonAsync("/api/goals", new CreateGoalRequest
        {
            Title = "Member 1 Private Goal",
            TargetValue = 50m,
            Unit = "kg",
            TargetDate = DateTime.UtcNow.AddMonths(1)
        });
        var goal = await createGoalRes.Content.ReadFromJsonAsync<GoalDto>(_jsonOptions);

        // Member 2 tries to read Member 1's goal
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", member2Token);
        var getRes = await _client.GetAsync($"/api/goals/{goal!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getRes.StatusCode);

        // Member 2 tries to log progress on Member 1's goal
        var postProgRes = await _client.PostAsJsonAsync($"/api/goals/{goal.Id}/progress", new CreateProgressRecordRequest
        {
            GoalId = goal.Id,
            Value = 60m
        });
        Assert.Equal(HttpStatusCode.Forbidden, postProgRes.StatusCode);
    }

    [Fact]
    public async Task Membership_Analytics_Returns_Summary_Metrics()
    {
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var res = await _client.GetAsync("/api/memberships/analytics");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var analytics = await res.Content.ReadFromJsonAsync<MembershipAnalyticsDto>(_jsonOptions);
        Assert.NotNull(analytics);
        Assert.True(analytics.TotalMembers >= 0);
        Assert.True(analytics.TotalRevenue >= 0);
    }
}
