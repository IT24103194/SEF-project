using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartGym.Api.Configuration;
using SmartGym.Api.DTOs.AI;
using SmartGym.Api.Middleware;
using SmartGym.Api.Services;
using Xunit;

namespace SmartGym.Api.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_handler(request));
    }
}

public class AiServiceClientTests
{
    private readonly IOptions<AiServiceSettings> _settings;

    public AiServiceClientTests()
    {
        _settings = Options.Create(new AiServiceSettings
        {
            BaseUrl = "http://localhost:8000",
            ApiKey = "test-internal-secret-token",
            ApprovalCostThreshold = 25000.0m,
            MaxRetries = 3,
            ToolTimeoutSeconds = 10
        });
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthResponse_WhenServiceHealthy()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/health", req.RequestUri?.AbsolutePath);
            Assert.True(req.Headers.Contains("X-Internal-Api-Key"));
            Assert.Equal("test-internal-secret-token", req.Headers.GetValues("X-Internal-Api-Key").First());

            var json = JsonSerializer.Serialize(new
            {
                status = "healthy",
                service = "smartgym-ai",
                version = "1.0.0"
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var httpContextAccessor = new HttpContextAccessor();
        var client = new AiServiceClient(httpClient, _settings, httpContextAccessor, NullLogger<AiServiceClient>.Instance);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("healthy", result!.Status);
        Assert.Equal("smartgym-ai", result.Service);
    }

    [Fact]
    public async Task StartWorkflowAsync_PropagatesCorrelationIdAndInternalApiKey()
    {
        // Arrange
        var expectedCorrelationId = "test-corr-12345";
        var expectedWfId = Guid.NewGuid();
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/internal/workflows/start", req.RequestUri?.AbsolutePath);
            Assert.Equal("test-internal-secret-token", req.Headers.GetValues("X-Internal-Api-Key").First());
            Assert.Equal(expectedCorrelationId, req.Headers.GetValues("X-Correlation-ID").First());

            var json = JsonSerializer.Serialize(new
            {
                id = expectedWfId,
                issue_id = Guid.NewGuid(),
                issue_title = "Motor Overheating",
                equipment_name = "Treadmill",
                workflow_type = "FacilityResolution",
                status = "Running",
                current_step = "evaluate_diagnosis",
                requires_human_approval = false,
                estimated_confidence_score = 0.95,
                started_at = DateTime.UtcNow
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var httpContextAccessor = new HttpContextAccessor();
        var client = new AiServiceClient(httpClient, _settings, httpContextAccessor, NullLogger<AiServiceClient>.Instance);

        var request = new AiWorkflowStartRequestDto
        {
            WorkflowType = "FacilityResolution",
            IssueId = Guid.NewGuid(),
            IssueTitle = "Motor Overheating",
            Description = "Motor overheating during high incline test",
            EquipmentName = "Treadmill"
        };

        // Act
        var result = await client.StartWorkflowAsync(request, correlationId: expectedCorrelationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedWfId, result!.Id);
        Assert.Equal("Running", result.Status);
    }

    [Fact]
    public async Task GetWorkflowAsync_ReturnsWorkflowState_WhenFound()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal($"/internal/workflows/{workflowId}", req.RequestUri?.AbsolutePath);

            var json = JsonSerializer.Serialize(new
            {
                id = workflowId,
                issue_id = Guid.NewGuid(),
                issue_title = "Motor Failure",
                equipment_name = "Treadmill",
                workflow_type = "FacilityResolution",
                status = "AwaitingApproval",
                current_step = "approval_gate",
                requires_human_approval = true,
                estimated_cost = 35000.0,
                started_at = DateTime.UtcNow
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var httpContextAccessor = new HttpContextAccessor();
        var client = new AiServiceClient(httpClient, _settings, httpContextAccessor, NullLogger<AiServiceClient>.Instance);

        // Act
        var result = await client.GetWorkflowAsync(workflowId.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workflowId, result!.Id);
        Assert.Equal("AwaitingApproval", result.Status);
        Assert.True(result.RequiresHumanApproval);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_SendsDecisionAndReturnsUpdatedState()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"/internal/workflows/{workflowId}/resume", req.RequestUri?.AbsolutePath);

            var json = JsonSerializer.Serialize(new
            {
                id = workflowId,
                issue_id = Guid.NewGuid(),
                workflow_type = "FacilityResolution",
                status = "Completed",
                current_step = "execution",
                requires_human_approval = false,
                started_at = DateTime.UtcNow
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var httpContextAccessor = new HttpContextAccessor();
        var client = new AiServiceClient(httpClient, _settings, httpContextAccessor, NullLogger<AiServiceClient>.Instance);

        // Act
        var result = await client.ResumeWorkflowAsync(workflowId.ToString(), new AiWorkflowResumeRequestDto
        {
            Action = "approve",
            Comments = "Cost within maintenance budget approved by manager"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result!.Status);
    }

    [Fact]
    public async Task CancelWorkflowAsync_SendsReasonAndReturnsCancelledState()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"/internal/workflows/{workflowId}/cancel", req.RequestUri?.AbsolutePath);

            var json = JsonSerializer.Serialize(new
            {
                id = workflowId,
                issue_id = Guid.NewGuid(),
                workflow_type = "FacilityResolution",
                status = "Cancelled",
                current_step = "Cancelled",
                started_at = DateTime.UtcNow
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var httpContextAccessor = new HttpContextAccessor();
        var client = new AiServiceClient(httpClient, _settings, httpContextAccessor, NullLogger<AiServiceClient>.Instance);

        // Act
        var result = await client.CancelWorkflowAsync(workflowId.ToString(), new AiWorkflowCancelRequestDto
        {
            Reason = "Duplicate issue filed"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cancelled", result!.Status);
    }

    [Fact]
    public async Task HttpContextCorrelationId_IsResolvedWhenExplicitNotPassed()
    {
        // Arrange
        var contextCorrelationId = "context-auto-gen-999";
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(contextCorrelationId, req.Headers.GetValues("X-Correlation-ID").First());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"healthy\",\"service\":\"smartgym-ai\",\"version\":\"1.0.0\"}", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = contextCorrelationId;
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var client = new AiServiceClient(httpClient, _settings, httpContextAccessor, NullLogger<AiServiceClient>.Instance);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
    }
}
