using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Controllers;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using SmartGym.Api.Entities;
using Xunit;

namespace SmartGym.Api.Tests;

public class AIWorkflowsApprovalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AIWorkflowsApprovalTests(WebApplicationFactory<Program> factory)
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

    private async Task<AIWorkflow> CreateTestWorkflowAsync(string payloadJson = "{\"estimatedCost\": 450.0, \"proposedAction\": \"Replace motor belt\"}")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var equipment = await db.Equipment.FirstOrDefaultAsync();
        if (equipment == null)
        {
            var location = new Location
            {
                Name = $"Studio {Guid.NewGuid():N}",
                Floor = "1st Floor"
            };
            await db.Locations.AddAsync(location);
            await db.SaveChangesAsync();

            equipment = new Equipment
            {
                Name = $"Treadmill {Guid.NewGuid():N}",
                SerialNumber = $"SN-{Guid.NewGuid():N}".Substring(0, 16),
                LocationId = location.Id,
                Status = EquipmentStatus.Operational
            };
            await db.Equipment.AddAsync(equipment);
            await db.SaveChangesAsync();
        }

        var member = await db.Members.FirstOrDefaultAsync();
        if (member == null)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "member@smartgym.com");
            member = new Member
            {
                UserId = user!.Id,
                DateOfBirth = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EmergencyContactName = "Jane Doe",
                EmergencyContactPhone = "555-1234"
            };
            await db.Members.AddAsync(member);
            await db.SaveChangesAsync();
        }

        var issue = new FacilityIssue
        {
            Title = $"Abnormal belt friction {Guid.NewGuid():N}",
            Description = "Loud screeching sound during high speed operation.",
            Severity = IssueSeverity.High,
            Status = FacilityIssueStatus.PENDING_APPROVAL,
            EquipmentId = equipment.Id,
            LocationId = equipment.LocationId,
            ReportedByMemberId = member.Id
        };
        await db.FacilityIssues.AddAsync(issue);
        await db.SaveChangesAsync();

        var workflow = new AIWorkflow
        {
            IssueId = issue.Id,
            WorkflowType = "FacilityIssueDiagnosis",
            Status = AIWorkflowStatus.AwaitingApproval,
            CurrentStep = "Awaiting human authorization",
            DiagnosisSummary = "Drive belt tensioner worn out",
            RecommendedAction = "Procure replacement belt and schedule technician",
            EstimatedConfidenceScore = 0.94,
            RequiresHumanApproval = true,
            HumanApprovalGranted = null,
            StructuredOutputPayloadJson = payloadJson
        };
        await db.AIWorkflows.AddAsync(workflow);
        await db.SaveChangesAsync();

        return workflow;
    }

    [Fact]
    public async Task Admin_Approval_Succeeds_And_Persists_Approval_And_AuditLog()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkflowApprovalDecisionRequest
        {
            Comments = "Approved for vendor order dispatch by facility administrator."
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkflowApprovalResultDto>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Equal(workflow.Id, result.WorkflowId);
        Assert.Equal("APPROVED", result.ApprovalStatus);
        Assert.True(result.HumanApprovalGranted);
        Assert.NotNull(result.ApprovalId);

        // Verify Database Persistence
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        var updatedWorkflow = await db.AIWorkflows.FindAsync(workflow.Id);
        Assert.NotNull(updatedWorkflow);
        Assert.True(updatedWorkflow.HumanApprovalGranted);
        Assert.Equal(AIWorkflowStatus.Executing, updatedWorkflow.Status);

        var updatedIssue = await db.FacilityIssues.FindAsync(workflow.IssueId);
        Assert.NotNull(updatedIssue);
        Assert.Equal(FacilityIssueStatus.APPROVED, updatedIssue.Status);

        var approvalRecord = await db.Approvals.FindAsync(result.ApprovalId);
        Assert.NotNull(approvalRecord);
        Assert.Equal(ApprovalDecision.Approved, approvalRecord.Decision);
        Assert.Equal(request.Comments, approvalRecord.Comments);

        var auditLog = await db.AuditLogs
            .Where(a => a.EntityName == "AIWorkflow" && a.EntityId == workflow.Id.ToString() && a.Action == "APPROVE")
            .FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
    }

    [Fact]
    public async Task Member_Approval_Fails_With_Forbidden()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync();
        var token = await GetMemberTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkflowApprovalDecisionRequest { Comments = "Member attempting approval" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify DB not mutated
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var untouchedWorkflow = await db.AIWorkflows.FindAsync(workflow.Id);
        Assert.Null(untouchedWorkflow!.HumanApprovalGranted);
        Assert.Equal(AIWorkflowStatus.AwaitingApproval, untouchedWorkflow.Status);
    }

    [Fact]
    public async Task Trainer_Approval_Fails_With_Forbidden()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync();
        var token = await GetTrainerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkflowApprovalDecisionRequest { Comments = "Trainer attempting approval" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify DB not mutated
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var untouchedWorkflow = await db.AIWorkflows.FindAsync(workflow.Id);
        Assert.Null(untouchedWorkflow!.HumanApprovalGranted);
        Assert.Equal(AIWorkflowStatus.AwaitingApproval, untouchedWorkflow.Status);
    }

    [Fact]
    public async Task Rejected_Request_Cannot_Execute_And_Records_Rejection()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkflowApprovalDecisionRequest
        {
            Comments = "Vendor quote is too expensive, equipment will be decommissioned."
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/reject", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkflowApprovalResultDto>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("REJECTED", result.ApprovalStatus);
        Assert.False(result.HumanApprovalGranted);

        // Verify DB status
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var updatedWorkflow = await db.AIWorkflows.FindAsync(workflow.Id);
        Assert.False(updatedWorkflow!.HumanApprovalGranted);
        Assert.Equal(AIWorkflowStatus.Failed, updatedWorkflow.Status);

        var updatedIssue = await db.FacilityIssues.FindAsync(workflow.IssueId);
        Assert.Equal(FacilityIssueStatus.REJECTED, updatedIssue!.Status);

        // Verify that executing/approving after rejection fails with Conflict
        var secondApproveResponse = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", request);
        Assert.Equal(HttpStatusCode.Conflict, secondApproveResponse.StatusCode);
    }

    [Fact]
    public async Task Approval_Replay_Fails_With_Conflict()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkflowApprovalDecisionRequest { Comments = "Initial approval" };

        // Act 1: Initial approval succeeds
        var firstResponse = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act 2: Replay of approval
        var secondResponse = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", request);

        // Assert: Replay is rejected with 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Revise_Request_Sets_Revision_Required_Status()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkflowApprovalDecisionRequest
        {
            Comments = "Need additional quotes from alternative suppliers."
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/revise", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkflowApprovalResultDto>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("REVISION_REQUIRED", result.ApprovalStatus);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();
        var updatedWorkflow = await db.AIWorkflows.FindAsync(workflow.Id);
        Assert.Null(updatedWorkflow!.HumanApprovalGranted);
        Assert.Equal(AIWorkflowStatus.Planning, updatedWorkflow.Status);

        var updatedIssue = await db.FacilityIssues.FindAsync(workflow.IssueId);
        Assert.Equal(FacilityIssueStatus.REVISION_REQUIRED, updatedIssue!.Status);
    }

    [Fact]
    public async Task Proposal_Integrity_Check_Fails_On_Corrupted_Or_Tampered_Payload()
    {
        // Arrange
        var workflow = await CreateTestWorkflowAsync(payloadJson: "{\"tampered\": true, \"modifiedAmount\": 999999.0}");
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ai-workflows/{workflow.Id}/approve", new WorkflowApprovalDecisionRequest());

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
