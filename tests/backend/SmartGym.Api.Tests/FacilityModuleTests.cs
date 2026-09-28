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
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class FacilityModuleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FacilityModuleTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task Location_CompleteCrudLifecycle_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create Location
        var createRequest = new CreateLocationRequest
        {
            Name = $"Pilates & Mobility Studio {Guid.NewGuid():N}",
            Floor = "2nd Floor",
            Description = "Equipped with reformer machines and balance balls."
        };

        var postResp = await _client.PostAsJsonAsync("/api/locations", createRequest);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
        var created = await postResp.Content.ReadFromJsonAsync<LocationDto>(_jsonOptions);
        Assert.NotNull(created);
        Assert.Equal(createRequest.Name, created.Name);

        // 2. Get Location By ID
        var getResp = await _client.GetAsync($"/api/locations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        // 3. Update Location
        var updateRequest = new UpdateLocationRequest
        {
            Name = created.Name + " - Updated",
            Floor = "2nd Floor West Wing",
            Description = "Updated description"
        };
        var putResp = await _client.PutAsJsonAsync($"/api/locations/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var updated = await putResp.Content.ReadFromJsonAsync<LocationDto>(_jsonOptions);
        Assert.Equal(updateRequest.Name, updated!.Name);

        // 4. Delete Location
        var delResp = await _client.DeleteAsync($"/api/locations/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
    }

    [Fact]
    public async Task Equipment_CompleteCrudAndMaintenanceHistory_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a Location first
        var locResp = await _client.PostAsJsonAsync("/api/locations", new CreateLocationRequest
        {
            Name = $"Crossfit Rig Zone {Guid.NewGuid():N}",
            Floor = "Ground Floor",
            Description = "Rigging and functional fitness."
        });
        var loc = await locResp.Content.ReadFromJsonAsync<LocationDto>(_jsonOptions);

        // 1. Create Equipment
        var createEq = new CreateEquipmentRequest
        {
            LocationId = loc!.Id,
            SerialNumber = $"EQ-{Guid.NewGuid():N}",
            Name = "Rogue Echo AirBike Pro",
            Model = "Echo-V3",
            Manufacturer = "Rogue Fitness",
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            Status = EquipmentStatus.Operational
        };

        var postResp = await _client.PostAsJsonAsync("/api/equipment", createEq);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
        var eq = await postResp.Content.ReadFromJsonAsync<EquipmentDto>(_jsonOptions);
        Assert.NotNull(eq);
        Assert.Equal(createEq.Name, eq.Name);

        // 2. Update Equipment Status
        var updateEq = new UpdateEquipmentRequest
        {
            LocationId = loc.Id,
            SerialNumber = eq.SerialNumber,
            Name = eq.Name,
            Model = eq.Model,
            Manufacturer = eq.Manufacturer,
            PurchaseDate = eq.PurchaseDate,
            Status = EquipmentStatus.NeedsMaintenance,
            LastServicedDate = DateTime.UtcNow
        };
        var putResp = await _client.PutAsJsonAsync($"/api/equipment/{eq.Id}", updateEq);
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var updated = await putResp.Content.ReadFromJsonAsync<EquipmentDto>(_jsonOptions);
        Assert.Equal(EquipmentStatus.NeedsMaintenance, updated!.Status);

        // 3. Maintenance History
        var histResp = await _client.GetAsync($"/api/equipment/{eq.Id}/history");
        Assert.Equal(HttpStatusCode.OK, histResp.StatusCode);
        var history = await histResp.Content.ReadFromJsonAsync<EquipmentHistoryDto>(_jsonOptions);
        Assert.NotNull(history);
        Assert.Equal(eq.Id, history.Equipment.Id);

        // 4. Delete Equipment
        var delResp = await _client.DeleteAsync($"/api/equipment/{eq.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
    }

    [Fact]
    public async Task FacilityIssue_DeterministicContentModeration_MasksProfanityAndLogsAudit()
    {
        var memberToken = await GetMemberTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        // Get an existing location
        var locsResp = await _client.GetAsync("/api/locations?pageSize=1");
        var locs = await locsResp.Content.ReadFromJsonAsync<PagedResult<LocationDto>>(_jsonOptions);
        var locationId = locs!.Items.First().Id;

        // Create an issue containing prohibited offensive terms ("damn", "shit", "idiot")
        var issueRequest = new CreateFacilityIssueRequest
        {
            LocationId = locationId,
            Title = "Broken Cable Machine Handle",
            Description = "This damn cable snapped and almost hit me! The staff are absolute idiot scammers for not fixing this shit!",
            Severity = IssueSeverity.High
        };

        var postResp = await _client.PostAsJsonAsync("/api/facility-issues", issueRequest);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
        var created = await postResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.NotNull(created);

        // Assert Content Moderation took place
        Assert.Equal("Flagged", created.ModerationStatus);
        Assert.NotNull(created.ModerationReason);
        Assert.Contains("offensive", created.ModerationReason, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(created.SanitizedDescription);

        // The sanitized description must mask the words with asterisks and not contain the raw profanity
        Assert.DoesNotContain("shit", created.SanitizedDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("*", created.SanitizedDescription);

        // Check history / audit trail
        var histResp = await _client.GetAsync($"/api/facility-issues/{created.Id}/history");
        Assert.Equal(HttpStatusCode.OK, histResp.StatusCode);
        var history = await histResp.Content.ReadFromJsonAsync<List<IssueHistoryDto>>(_jsonOptions);
        Assert.NotNull(history);
        Assert.Contains(history, h => h.Action == "CONTENT_MODERATION" || h.Action == "ISSUE_SUBMITTED");
    }

    [Fact]
    public async Task FacilityIssue_ImageValidation_RejectsInvalidExtensionAndOversizedFiles()
    {
        var memberToken = await GetMemberTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        // Get location
        var locsResp = await _client.GetAsync("/api/locations?pageSize=1");
        var locs = await locsResp.Content.ReadFromJsonAsync<PagedResult<LocationDto>>(_jsonOptions);
        var locationId = locs!.Items.First().Id;

        // 1. Create a clean issue
        var postResp = await _client.PostAsJsonAsync("/api/facility-issues", new CreateFacilityIssueRequest
        {
            LocationId = locationId,
            Title = "Loose Dumbbell Collar",
            Description = "Collar screw on 24kg dumbbell is loose and spins freely.",
            Severity = IssueSeverity.Medium
        });
        var issue = await postResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);

        // 2. Test Invalid File Extension (.exe)
        var invalidContent = new MultipartFormDataContent();
        var fakeExeBytes = Encoding.UTF8.GetBytes("MZ fake executable header");
        var fileContent = new ByteArrayContent(fakeExeBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        invalidContent.Add(fileContent, "file", "malware.exe");

        var badExtResp = await _client.PostAsync($"/api/facility-issues/{issue!.Id}/images", invalidContent);
        Assert.Equal(HttpStatusCode.BadRequest, badExtResp.StatusCode);

        // 3. Test Oversized File (> 5MB)
        var oversizedContent = new MultipartFormDataContent();
        var largeBytes = new byte[6 * 1024 * 1024]; // 6 MB
        new Random().NextBytes(largeBytes);
        var largeFile = new ByteArrayContent(largeBytes);
        largeFile.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        oversizedContent.Add(largeFile, "file", "oversized_photo.png");

        var oversizedResp = await _client.PostAsync($"/api/facility-issues/{issue.Id}/images", oversizedContent);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedResp.StatusCode);

        // 4. Test Valid Image Upload (.png)
        var validContent = new MultipartFormDataContent();
        var validPngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var validFile = new ByteArrayContent(validPngHeader);
        validFile.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        validContent.Add(validFile, "file", "valid_equipment_photo.png");

        var validResp = await _client.PostAsync($"/api/facility-issues/{issue.Id}/images", validContent);
        Assert.Equal(HttpStatusCode.Created, validResp.StatusCode);
        var imgDto = await validResp.Content.ReadFromJsonAsync<IssueImageDto>(_jsonOptions);
        Assert.NotNull(imgDto);
        Assert.Contains("/uploads/issues/", imgDto.ImageUrl);
    }

    [Fact]
    public async Task FacilityIssue_ValidStateTransitions_AndResolutionNotesEnforcement()
    {
        var adminToken = await GetAdminTokenAsync();
        var memberToken = await GetMemberTokenAsync();

        // 1. Member creates issue (Starts at SUBMITTED)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var locsResp = await _client.GetAsync("/api/locations?pageSize=1");
        var locs = await locsResp.Content.ReadFromJsonAsync<PagedResult<LocationDto>>(_jsonOptions);
        var locationId = locs!.Items.First().Id;

        var postResp = await _client.PostAsJsonAsync("/api/facility-issues", new CreateFacilityIssueRequest
        {
            LocationId = locationId,
            Title = "Rowing Machine Handle Strap Frayed",
            Description = "Handle strap on Concept2 rower is severely frayed near the junction.",
            Severity = IssueSeverity.High
        });
        var issue = await postResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.Equal(FacilityIssueStatus.SUBMITTED, issue!.Status);

        // 2. Admin advances: SUBMITTED -> APPROVED
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var appResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.APPROVED,
            Comments = "Approved for vendor dispatch."
        });
        Assert.Equal(HttpStatusCode.OK, appResp.StatusCode);
        var appIssue = await appResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.Equal(FacilityIssueStatus.APPROVED, appIssue!.Status);

        // 3. Admin advances: APPROVED -> VENDOR_CONTACTED
        var vendorResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.VENDOR_CONTACTED,
            Comments = "Contacted Apex Gym Spares for OEM strap replacement."
        });
        Assert.Equal(HttpStatusCode.OK, vendorResp.StatusCode);

        // 4. Admin advances: VENDOR_CONTACTED -> REPAIR_SCHEDULED
        var schedResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.REPAIR_SCHEDULED,
            Comments = "Technician scheduled for tomorrow 10:00 AM."
        });
        Assert.Equal(HttpStatusCode.OK, schedResp.StatusCode);

        // 5. Admin advances: REPAIR_SCHEDULED -> IN_PROGRESS
        var inProgResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.IN_PROGRESS,
            Comments = "Technician on site installing new strap."
        });
        Assert.Equal(HttpStatusCode.OK, inProgResp.StatusCode);

        // 6. Business Rule: Transition to RESOLVED WITHOUT resolution notes MUST FAIL
        var emptyResResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.RESOLVED,
            ResolutionNotes = "" // Empty should fail
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyResResp.StatusCode);

        // 7. Transition to RESOLVED WITH resolution notes SUCCEEDS
        var resolvedResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.RESOLVED,
            ResolutionNotes = "Replaced frayed handle strap with OEM Concept2 nickel-plated chain and strap assembly. Tested under 200W pull."
        });
        Assert.Equal(HttpStatusCode.OK, resolvedResp.StatusCode);
        var finalIssue = await resolvedResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.Equal(FacilityIssueStatus.RESOLVED, finalIssue!.Status);
        Assert.NotNull(finalIssue.ResolvedAt);
        Assert.NotNull(finalIssue.ResolutionNotes);
    }

    [Fact]
    public async Task FacilityIssue_MemberCannotTransitionToProtectedStates()
    {
        var memberToken = await GetMemberTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var locsResp = await _client.GetAsync("/api/locations?pageSize=1");
        var locs = await locsResp.Content.ReadFromJsonAsync<PagedResult<LocationDto>>(_jsonOptions);
        var locationId = locs!.Items.First().Id;

        var postResp = await _client.PostAsJsonAsync("/api/facility-issues", new CreateFacilityIssueRequest
        {
            LocationId = locationId,
            Title = "Spin Bike Pedaling Resistance Stuck",
            Description = "Resistance knob does not increase tension.",
            Severity = IssueSeverity.Medium
        });
        var issue = await postResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);

        // Member attempts unauthorized transition to RESOLVED
        var attemptResp = await _client.PostAsJsonAsync($"/api/facility-issues/{issue!.Id}/status", new IssueStatusTransitionRequest
        {
            NewStatus = FacilityIssueStatus.RESOLVED,
            ResolutionNotes = "I fixed it myself."
        });

        Assert.Equal(HttpStatusCode.Forbidden, attemptResp.StatusCode);
    }

    [Fact]
    public async Task FacilityIssue_MemberScoping_MemberOnlySeesOwnIssues_AdminSeesAll()
    {
        var memberToken = await GetMemberTokenAsync();
        var adminToken = await GetAdminTokenAsync();

        // Admin sees all issues
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminResp = await _client.GetAsync("/api/facility-issues");
        Assert.Equal(HttpStatusCode.OK, adminResp.StatusCode);
        var adminIssues = await adminResp.Content.ReadFromJsonAsync<PagedResult<FacilityIssueDto>>(_jsonOptions);
        Assert.NotNull(adminIssues);
        Assert.True(adminIssues.TotalCount >= 1);

        // Member queries issues
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var memberResp = await _client.GetAsync("/api/facility-issues");
        Assert.Equal(HttpStatusCode.OK, memberResp.StatusCode);
        var memberIssues = await memberResp.Content.ReadFromJsonAsync<PagedResult<FacilityIssueDto>>(_jsonOptions);
        Assert.NotNull(memberIssues);

        // Every issue returned to Member must belong to this member
        foreach (var item in memberIssues.Items)
        {
            Assert.Equal("Nuwan Perera", item.ReporterName);
        }
    }

    [Fact]
    public async Task RepairOrder_HighValue_IsFlaggedForApproval_AndAdminCanApprove()
    {
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Get an existing issue and equipment
        var locsResp = await _client.GetAsync("/api/locations?pageSize=1");
        var locs = await locsResp.Content.ReadFromJsonAsync<PagedResult<LocationDto>>(_jsonOptions);
        var locationId = locs!.Items.First().Id;

        var eqResp = await _client.PostAsJsonAsync("/api/equipment", new CreateEquipmentRequest
        {
            LocationId = locationId,
            SerialNumber = $"EQ-HV-{Guid.NewGuid():N}",
            Name = "NordicTrack Commercial Treadmill",
            Model = "NT-9000",
            Manufacturer = "NordicTrack",
            Status = EquipmentStatus.UnderRepair
        });
        var eq = await eqResp.Content.ReadFromJsonAsync<EquipmentDto>(_jsonOptions);

        var issueResp = await _client.PostAsJsonAsync("/api/facility-issues", new CreateFacilityIssueRequest
        {
            LocationId = locationId,
            EquipmentId = eq!.Id,
            Title = "Incline Motor Burnout",
            Description = "Incline actuator motor burnt out during high incline session.",
            Severity = IssueSeverity.Critical
        });
        var issue = await issueResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);

        // 2. Create High-Value Repair Order (> $500 threshold, e.g. $1,200)
        var roRequest = new CreateRepairOrderRequest
        {
            IssueId = issue!.Id,
            EquipmentId = eq.Id,
            EstimatedCost = 1200.00m,
            TechnicianName = "Kamal Gunaratne (Certified Tech)",
            Items = new List<CreateRepairOrderItemRequest>
            {
                new() { PartName = "Incline Drive Actuator Motor", PartNumber = "NT-ACT-99", Quantity = 1, UnitCost = 900.00m },
                new() { PartName = "Heavy Duty Motor Control Board", PartNumber = "NT-MCB-12", Quantity = 1, UnitCost = 300.00m }
            }
        };

        var roPostResp = await _client.PostAsJsonAsync("/api/repair-orders", roRequest);
        Assert.Equal(HttpStatusCode.Created, roPostResp.StatusCode);
        var ro = await roPostResp.Content.ReadFromJsonAsync<RepairOrderDto>(_jsonOptions);
        Assert.NotNull(ro);

        // Business Rule: High-value repair is held for approval
        Assert.True(ro.RequiresApproval);
        Assert.Equal(RepairOrderStatus.PendingApproval, ro.Status);

        // Associated issue is moved to PENDING_APPROVAL
        var checkIssueResp = await _client.GetAsync($"/api/facility-issues/{issue.Id}");
        var checkIssue = await checkIssueResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.Equal(FacilityIssueStatus.PENDING_APPROVAL, checkIssue!.Status);

        // 3. Admin Approves High-Value Repair
        var approvalResp = await _client.PostAsJsonAsync($"/api/repair-orders/{ro.Id}/approve", new ProcessApprovalRequest
        {
            Decision = ApprovalDecision.Approved,
            Comments = "Budget authorized under Q3 facility maintenance allocation."
        });
        Assert.Equal(HttpStatusCode.OK, approvalResp.StatusCode);
        var approvedRo = await approvalResp.Content.ReadFromJsonAsync<RepairOrderDto>(_jsonOptions);
        Assert.Equal(RepairOrderStatus.Approved, approvedRo!.Status);

        // Associated issue is moved to APPROVED
        var finalIssueResp = await _client.GetAsync($"/api/facility-issues/{issue.Id}");
        var finalIssue = await finalIssueResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.Equal(FacilityIssueStatus.APPROVED, finalIssue!.Status);
    }

    [Fact]
    public async Task Feedback_SubmitAndRespond_Succeeds()
    {
        var memberToken = await GetMemberTokenAsync();
        var adminToken = await GetAdminTokenAsync();

        // 1. Member submits feedback
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var createFeedback = new CreateFeedbackRequest
        {
            Subject = "Air conditioning temperature in Studio 1",
            Content = "The studio gets too humid during the 6 PM spin classes. Could the thermostat be lowered?",
            Rating = 4
        };

        var postResp = await _client.PostAsJsonAsync("/api/feedback", createFeedback);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
        var feedback = await postResp.Content.ReadFromJsonAsync<FeedbackDto>(_jsonOptions);
        Assert.NotNull(feedback);
        Assert.Equal(createFeedback.Subject, feedback.Subject);

        // 2. Admin responds
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var respondResp = await _client.PostAsJsonAsync($"/api/feedback/{feedback.Id}/respond", new RespondFeedbackRequest
        {
            AdminResponse = "Thank you for letting us know! We have adjusted the HVAC schedule to pre-cool Studio 1 starting at 5:30 PM.",
            Status = FeedbackStatus.Reviewed
        });
        Assert.Equal(HttpStatusCode.OK, respondResp.StatusCode);
        var responded = await respondResp.Content.ReadFromJsonAsync<FeedbackDto>(_jsonOptions);
        Assert.NotNull(responded!.AdminResponse);
        Assert.Equal(FeedbackStatus.Reviewed, responded.Status);
    }

    [Fact]
    public async Task AcceptanceScenario_MemberReportsIssue_WithEquipmentPhoto_StoredInPostgreSQL_AndVisibleToReactAdmin()
    {
        // 1. Member authenticates in Flutter
        var memberToken = await GetMemberTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        // Fetch location and equipment
        var locsResp = await _client.GetAsync("/api/locations?pageSize=1");
        var locs = await locsResp.Content.ReadFromJsonAsync<PagedResult<LocationDto>>(_jsonOptions);
        var locationId = locs!.Items.First().Id;

        // 2 & 3. Member selects/takes equipment photo and submits issue description
        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(new StringContent(locationId.ToString()), "locationId");
        multipartContent.Add(new StringContent("Cable Pulley Worn & Sticking"), "title");
        multipartContent.Add(new StringContent("The top pulley on the cable machine is squeaking loudly and sticking under load."), "description");
        multipartContent.Add(new StringContent("High"), "severity");

        // Equipment photo file (JPG payload)
        var fakeJpgBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var fileContent = new ByteArrayContent(fakeJpgBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipartContent.Add(fileContent, "image", "cable_pulley_damaged.jpg");

        // 4. API stores it
        var submitResp = await _client.PostAsync("/api/facility-issues/with-image", multipartContent);
        Assert.Equal(HttpStatusCode.Created, submitResp.StatusCode);
        var createdIssue = await submitResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.NotNull(createdIssue);
        Assert.Equal("Cable Pulley Worn & Sticking", createdIssue.Title);
        Assert.Equal(FacilityIssueStatus.SUBMITTED, createdIssue.Status);

        // 5. PostgreSQL stores it (verify via DbContext)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
            var dbIssue = await db.FacilityIssues.FindAsync(createdIssue.Id);
            Assert.NotNull(dbIssue);
            Assert.Equal("Cable Pulley Worn & Sticking", dbIssue.Title);

            // Verify issue image was stored in PostgreSQL
            var dbImages = db.IssueImages.Where(img => img.IssueId == createdIssue.Id).ToList();
            Assert.Single(dbImages);
            Assert.Contains("/uploads/issues/", dbImages[0].ImageUrl);
            Assert.Equal("image/jpeg", dbImages[0].ContentType);

            // Verify issue history (AuditLog) was created in PostgreSQL
            var issueIdStr = createdIssue.Id.ToString();
            var dbAudits = db.AuditLogs.Where(a => a.EntityName == "FacilityIssue" && a.EntityId == issueIdStr).ToList();
            Assert.NotEmpty(dbAudits);
            Assert.Contains(dbAudits, a => a.Action == "ISSUE_SUBMITTED");
        }

        // 6. React Admin sees it
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var adminGetResp = await _client.GetAsync($"/api/facility-issues/{createdIssue.Id}");
        Assert.Equal(HttpStatusCode.OK, adminGetResp.StatusCode);
        var adminViewIssue = await adminGetResp.Content.ReadFromJsonAsync<FacilityIssueDto>(_jsonOptions);
        Assert.NotNull(adminViewIssue);
        Assert.Equal(createdIssue.Id, adminViewIssue.Id);
        Assert.Equal("Cable Pulley Worn & Sticking", adminViewIssue.Title);
        Assert.Single(adminViewIssue.Images);

        // 7. Issue history is accessible to Admin
        var adminHistResp = await _client.GetAsync($"/api/facility-issues/{createdIssue.Id}/history");
        Assert.Equal(HttpStatusCode.OK, adminHistResp.StatusCode);
        var historyList = await adminHistResp.Content.ReadFromJsonAsync<List<IssueHistoryDto>>(_jsonOptions);
        Assert.NotNull(historyList);
        Assert.NotEmpty(historyList);
        Assert.Contains(historyList, h => h.Action == "ISSUE_SUBMITTED");
    }
}
