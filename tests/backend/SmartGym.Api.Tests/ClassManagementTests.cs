using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class ClassManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ClassManagementTests(WebApplicationFactory<Program> factory)
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

    private async Task<(User Trainer, string Token)> CreateIsolatedTrainerAsync(SmartGymDbContext db)
    {
        var trainer = new User
        {
            Email = $"trainer_{Guid.NewGuid():N}@smartgym.com",
            FirstName = "Iso",
            LastName = "Trainer",
            PhoneNumber = "+94710000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trainer123!"),
            IsActive = true
        };
        db.Users.Add(trainer);
        await db.SaveChangesAsync();

        var trainerRole = await db.Roles.FirstAsync(r => r.Name == "Trainer");
        db.UserRoles.Add(new UserRole { UserId = trainer.Id, RoleId = trainerRole.Id });
        await db.SaveChangesAsync();

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = trainer.Email,
            Password = "Trainer123!"
        });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        return (trainer, auth!.AccessToken);
    }

    private async Task EnsureMemberHasActiveMembershipAsync(SmartGymDbContext db, DateTime date)
    {
        var member = await db.Members.Include(m => m.Memberships).FirstAsync(m => m.User.Email == "member@smartgym.com");
        var hasCovering = member.Memberships.Any(m =>
            m.Status == MembershipStatus.Active &&
            m.StartDate <= date.Date &&
            m.EndDate >= date.Date);

        if (!hasCovering)
        {
            var plan = await db.MembershipPlans.FirstAsync();
            db.Memberships.Add(new Membership
            {
                MemberId = member.Id,
                PlanId = plan.Id,
                StartDate = date.AddDays(-10),
                EndDate = date.AddDays(60),
                Status = MembershipStatus.Active,
                PricePaid = 15000m
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAllClasses_ReturnsSuccessAndPagedResult()
    {
        var response = await _client.GetAsync("/api/fitness-classes?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<FitnessClassDto>>(_jsonOptions);
        Assert.NotNull(paged);
        Assert.NotEmpty(paged.Items);
    }

    [Fact]
    public async Task CreateClassCategory_AsAdmin_Succeeds()
    {
        var adminToken = await GetAdminTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/class-categories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var uniqueName = $"Pilates & Core {Guid.NewGuid().ToString()[..6]}";
        request.Content = JsonContent.Create(new CreateClassCategoryRequest
        {
            Name = uniqueName,
            Description = "Pilates reformer and deep core activation routines."
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ClassCategoryDto>(_jsonOptions);
        Assert.NotNull(created);
        Assert.Equal(uniqueName, created.Name);
    }

    [Fact]
    public async Task CreateSchedule_WithConflictingTrainerTime_FailsWithConflict()
    {
        var adminToken = await GetAdminTokenAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var (trainer, _) = await CreateIsolatedTrainerAsync(db);
        var fitnessClass = await db.FitnessClasses.FirstAsync();

        var startTime = DateTime.UtcNow.AddDays(10).Date.AddHours(9);
        var endTime = startTime.AddHours(1);

        // 1. Create schedule 1
        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req1.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Studio 1",
            StartTime = startTime,
            EndTime = endTime,
            Capacity = 15
        });
        var resp1 = await _client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);

        // 2. Try to create schedule 2 for same trainer with overlapping time (start 9:30, end 10:30)
        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req2.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Studio 2",
            StartTime = startTime.AddMinutes(30),
            EndTime = endTime.AddMinutes(30),
            Capacity = 10
        });
        var resp2 = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_WithInvalidDates_FailsWithBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var (trainer, _) = await CreateIsolatedTrainerAsync(db);
        var fitnessClass = await db.FitnessClasses.FirstAsync();

        // EndTime before StartTime
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Studio 1",
            StartTime = DateTime.UtcNow.AddDays(5).AddHours(10),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(9), // Invalid
            Capacity = 15
        });

        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task BookClass_FullCapacity_FailsWithConflict()
    {
        var adminToken = await GetAdminTokenAsync();
        var memberToken = await GetMemberTokenAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var (trainer, _) = await CreateIsolatedTrainerAsync(db);
        var fitnessClass = await db.FitnessClasses.FirstAsync();

        var startTime = DateTime.UtcNow.AddDays(15).Date.AddHours(14);
        await EnsureMemberHasActiveMembershipAsync(db, startTime);

        // Create schedule with Capacity = 1
        using var reqSchedule = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        reqSchedule.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        reqSchedule.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Studio 1",
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Capacity = 1
        });
        var respSchedule = await _client.SendAsync(reqSchedule);
        Assert.Equal(HttpStatusCode.Created, respSchedule.StatusCode);
        var schedule = await respSchedule.Content.ReadFromJsonAsync<ClassScheduleDto>(_jsonOptions);

        // First booking succeeds
        using var reqBook1 = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        reqBook1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        reqBook1.Content = JsonContent.Create(new CreateBookingRequest
        {
            ScheduleId = schedule!.Id
        });
        var respBook1 = await _client.SendAsync(reqBook1);
        Assert.Equal(HttpStatusCode.Created, respBook1.StatusCode);

        // Try second booking on full class (create a second member with active membership)
        var user2 = new User
        {
            Email = $"member2_{Guid.NewGuid():N}@smartgym.com",
            FirstName = "Saman",
            LastName = "Kumara",
            PhoneNumber = "+94770000001",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member123!"),
            IsActive = true
        };
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var memberRole = await db.Roles.FirstAsync(r => r.Name == "Member");
        db.UserRoles.Add(new UserRole { UserId = user2.Id, RoleId = memberRole.Id });

        var member2 = new Member { UserId = user2.Id };
        db.Members.Add(member2);
        await db.SaveChangesAsync();

        var plan = await db.MembershipPlans.FirstAsync();
        db.Memberships.Add(new Membership
        {
            MemberId = member2.Id,
            PlanId = plan.Id,
            StartDate = startTime.AddDays(-5),
            EndDate = startTime.AddDays(90),
            Status = MembershipStatus.Active,
            PricePaid = 10000m
        });
        await db.SaveChangesAsync();

        // Login as member2
        var login2 = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = user2.Email,
            Password = "Member123!"
        });
        var auth2 = await login2.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);

        using var reqBook2 = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        reqBook2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.AccessToken);
        reqBook2.Content = JsonContent.Create(new CreateBookingRequest
        {
            ScheduleId = schedule.Id
        });
        var respBook2 = await _client.SendAsync(reqBook2);
        Assert.Equal(HttpStatusCode.Conflict, respBook2.StatusCode);
    }

    [Fact]
    public async Task BookClass_DuplicateBooking_FailsWithConflict()
    {
        var adminToken = await GetAdminTokenAsync();
        var memberToken = await GetMemberTokenAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var (trainer, _) = await CreateIsolatedTrainerAsync(db);
        var fitnessClass = await db.FitnessClasses.FirstAsync();

        var startTime = DateTime.UtcNow.AddDays(18).Date.AddHours(8);
        await EnsureMemberHasActiveMembershipAsync(db, startTime);

        // Create schedule with Capacity = 10
        using var reqSchedule = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        reqSchedule.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        reqSchedule.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Studio 1",
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Capacity = 10
        });
        var respSchedule = await _client.SendAsync(reqSchedule);
        Assert.Equal(HttpStatusCode.Created, respSchedule.StatusCode);
        var schedule = await respSchedule.Content.ReadFromJsonAsync<ClassScheduleDto>(_jsonOptions);

        // Booking 1
        using var reqBook1 = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        reqBook1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        reqBook1.Content = JsonContent.Create(new CreateBookingRequest { ScheduleId = schedule!.Id });
        var respBook1 = await _client.SendAsync(reqBook1);
        Assert.Equal(HttpStatusCode.Created, respBook1.StatusCode);

        // Booking 2 by same member
        using var reqBook2 = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        reqBook2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        reqBook2.Content = JsonContent.Create(new CreateBookingRequest { ScheduleId = schedule.Id });
        var respBook2 = await _client.SendAsync(reqBook2);
        Assert.Equal(HttpStatusCode.Conflict, respBook2.StatusCode);
    }

    [Fact]
    public async Task BookClass_ExpiredMembership_FailsWithConflict()
    {
        // Create user with expired membership
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var userExpired = new User
        {
            Email = $"expired_{Guid.NewGuid():N}@smartgym.com",
            FirstName = "Expired",
            LastName = "User",
            PhoneNumber = "+94770000009",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member123!"),
            IsActive = true
        };
        db.Users.Add(userExpired);
        await db.SaveChangesAsync();

        var memberRole = await db.Roles.FirstAsync(r => r.Name == "Member");
        db.UserRoles.Add(new UserRole { UserId = userExpired.Id, RoleId = memberRole.Id });

        var member = new Member { UserId = userExpired.Id };
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var plan = await db.MembershipPlans.FirstAsync();
        // Add expired membership
        db.Memberships.Add(new Membership
        {
            MemberId = member.Id,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-10), // Expired!
            Status = MembershipStatus.Expired,
            PricePaid = 5000m
        });
        await db.SaveChangesAsync();

        var schedule = await db.ClassSchedules.FirstAsync(s => s.Status == ScheduleStatus.Scheduled && s.StartTime > DateTime.UtcNow);

        // Login as expired member
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = userExpired.Email,
            Password = "Member123!"
        });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        req.Content = JsonContent.Create(new CreateBookingRequest { ScheduleId = schedule.Id });

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_EnforcesPolicyAndDecrementsBookedCount()
    {
        var adminToken = await GetAdminTokenAsync();
        var memberToken = await GetMemberTokenAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var (trainer, _) = await CreateIsolatedTrainerAsync(db);
        var fitnessClass = await db.FitnessClasses.FirstAsync();

        var startTime = DateTime.UtcNow.AddDays(22).Date.AddHours(11);
        await EnsureMemberHasActiveMembershipAsync(db, startTime);

        using var reqSchedule = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        reqSchedule.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        reqSchedule.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Studio 1",
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Capacity = 10
        });
        var respSchedule = await _client.SendAsync(reqSchedule);
        Assert.Equal(HttpStatusCode.Created, respSchedule.StatusCode);
        var schedule = await respSchedule.Content.ReadFromJsonAsync<ClassScheduleDto>(_jsonOptions);

        // 1. Member books
        using var reqBook = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        reqBook.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        reqBook.Content = JsonContent.Create(new CreateBookingRequest { ScheduleId = schedule!.Id });
        var respBook = await _client.SendAsync(reqBook);
        Assert.Equal(HttpStatusCode.Created, respBook.StatusCode);
        var booking = await respBook.Content.ReadFromJsonAsync<BookingDto>(_jsonOptions);

        // Verify BookedCount = 1
        var avail1 = await _client.GetFromJsonAsync<ScheduleAvailabilityDto>($"/api/class-schedules/{schedule.Id}/availability", _jsonOptions);
        Assert.Equal(1, avail1!.BookedCount);

        // 2. Member cancels
        using var reqCancel = new HttpRequestMessage(HttpMethod.Post, $"/api/bookings/{booking!.Id}/cancel");
        reqCancel.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        reqCancel.Content = JsonContent.Create(new CancelBookingRequest { Reason = "Work meeting conflict" });
        var respCancel = await _client.SendAsync(reqCancel);
        Assert.Equal(HttpStatusCode.OK, respCancel.StatusCode);

        var cancelledBooking = await respCancel.Content.ReadFromJsonAsync<BookingDto>(_jsonOptions);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking!.Status);
        Assert.Equal("Work meeting conflict", cancelledBooking.CancellationReason);

        // Verify BookedCount decremented to 0
        var avail2 = await _client.GetFromJsonAsync<ScheduleAvailabilityDto>($"/api/class-schedules/{schedule.Id}/availability", _jsonOptions);
        Assert.Equal(0, avail2!.BookedCount);
    }

    [Fact]
    public async Task FullAcceptanceScenario_MemberBooks_TrainerViews_AttendanceRecorded()
    {
        var adminToken = await GetAdminTokenAsync();
        var memberToken = await GetMemberTokenAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var (trainer, trainerToken) = await CreateIsolatedTrainerAsync(db);
        var fitnessClass = await db.FitnessClasses.FirstAsync();

        var startTime = DateTime.UtcNow.AddDays(25).Date.AddHours(16);
        await EnsureMemberHasActiveMembershipAsync(db, startTime);

        // 1. Admin/Trainer creates a schedule
        using var reqSchedule = new HttpRequestMessage(HttpMethod.Post, "/api/class-schedules");
        reqSchedule.Headers.Authorization = new AuthenticationHeaderValue("Bearer", trainerToken);
        reqSchedule.Content = JsonContent.Create(new CreateClassScheduleRequest
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = "Main Hall",
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Capacity = 25
        });
        var respSchedule = await _client.SendAsync(reqSchedule);
        Assert.Equal(HttpStatusCode.Created, respSchedule.StatusCode);
        var schedule = await respSchedule.Content.ReadFromJsonAsync<ClassScheduleDto>(_jsonOptions);

        // 2. Member views schedules & books class
        using var reqBook = new HttpRequestMessage(HttpMethod.Post, "/api/bookings");
        reqBook.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        reqBook.Content = JsonContent.Create(new CreateBookingRequest { ScheduleId = schedule!.Id });
        var respBook = await _client.SendAsync(reqBook);
        Assert.Equal(HttpStatusCode.Created, respBook.StatusCode);
        var booking = await respBook.Content.ReadFromJsonAsync<BookingDto>(_jsonOptions);
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        // 3. PostgreSQL verified: booked count changed
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
            var dbSchedule = await verifyDb.ClassSchedules.FindAsync(schedule.Id);
            Assert.NotNull(dbSchedule);
            Assert.Equal(1, dbSchedule.BookedCount);

            var dbBooking = await verifyDb.Bookings.FindAsync(booking.Id);
            Assert.NotNull(dbBooking);
            Assert.Equal(BookingStatus.Confirmed, dbBooking.Status);
        }

        // 4. Trainer sees booking on attendance sheet
        using var reqSheet = new HttpRequestMessage(HttpMethod.Get, $"/api/attendances/schedule/{schedule.Id}");
        reqSheet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", trainerToken);
        var respSheet = await _client.SendAsync(reqSheet);
        Assert.Equal(HttpStatusCode.OK, respSheet.StatusCode);

        var sheet = await respSheet.Content.ReadFromJsonAsync<List<BookingDto>>(_jsonOptions);
        Assert.NotNull(sheet);
        Assert.Contains(sheet, b => b.Id == booking.Id);

        // 5. Trainer marks attendance as Attended
        using var reqAtt = new HttpRequestMessage(HttpMethod.Post, "/api/attendances");
        reqAtt.Headers.Authorization = new AuthenticationHeaderValue("Bearer", trainerToken);
        reqAtt.Content = JsonContent.Create(new RecordAttendanceRequest
        {
            BookingId = booking.Id,
            Status = AttendanceStatus.Attended
        });
        var respAtt = await _client.SendAsync(reqAtt);
        Assert.Equal(HttpStatusCode.OK, respAtt.StatusCode);

        var att = await respAtt.Content.ReadFromJsonAsync<AttendanceDto>(_jsonOptions);
        Assert.NotNull(att);
        Assert.Equal(AttendanceStatus.Attended, att.Status);
        Assert.Equal(booking.Id, att.BookingId);

        // Verify in DB
        using (var finalScope = _factory.Services.CreateScope())
        {
            var finalDb = finalScope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
            var dbAttendance = await finalDb.Attendances.FirstOrDefaultAsync(a => a.BookingId == booking.Id);
            Assert.NotNull(dbAttendance);
            Assert.Equal(AttendanceStatus.Attended, dbAttendance.Status);
            Assert.Equal(trainer.Id, dbAttendance.MarkedByUserId);
        }
    }
}
