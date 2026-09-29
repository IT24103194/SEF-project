using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartGym.Api.Configuration;
using SmartGym.Api.DTOs.AI;
using SmartGym.Api.Middleware;

namespace SmartGym.Api.Services;

/// <summary>
/// Production HTTP client implementation for communicating with the internal Python FastAPI AI microservice.
/// Ensures that internal API secrets and correlation IDs are attached on every call.
/// </summary>
public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly AiServiceSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AiServiceClient> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public const string InternalApiKeyHeader = "X-Internal-Api-Key";
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public AiServiceClient(
        HttpClient httpClient,
        IOptions<AiServiceSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AiServiceClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _settings.ToolTimeoutSeconds * 3));
    }

    private string ResolveCorrelationId(string? explicitCorrelationId)
    {
        if (!string.IsNullOrWhiteSpace(explicitCorrelationId))
        {
            return explicitCorrelationId;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var correlationObj) == true 
            && correlationObj is string correlationId && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid().ToString("N");
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string path, string? correlationId = null)
    {
        var resolvedCorrelationId = ResolveCorrelationId(correlationId);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(InternalApiKeyHeader, _settings.ApiKey);
        request.Headers.Add(CorrelationIdHeader, resolvedCorrelationId);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    public async Task<AiHealthResponseDto?> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateAuthenticatedRequest(HttpMethod.Get, "/health");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI microservice health check returned status code {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AiHealthResponseDto>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach AI microservice health endpoint.");
            return null;
        }
    }

    public async Task<AiWorkflowStateResponseDto?> StartWorkflowAsync(
        AiWorkflowStartRequestDto requestDto, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/internal/workflows/start", correlationId);
            var jsonPayload = JsonSerializer.Serialize(requestDto, _jsonOptions);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation("Starting AI workflow for IssueId {IssueId}, Type {WorkflowType}", 
                requestDto.IssueId, requestDto.WorkflowType);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to start AI workflow. Status: {StatusCode}, Response: {Content}", 
                    response.StatusCode, content);
                throw new HttpRequestException($"AI service error: {response.StatusCode} - {content}");
            }

            return JsonSerializer.Deserialize<AiWorkflowStateResponseDto>(content, _jsonOptions);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception when calling AI start workflow for IssueId {IssueId}", requestDto.IssueId);
            throw;
        }
    }

    public async Task<AiWorkflowStateResponseDto?> GetWorkflowAsync(
        string workflowId, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/internal/workflows/{workflowId}", correlationId);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve AI workflow {WorkflowId}. Status: {StatusCode}", workflowId, response.StatusCode);
                throw new HttpRequestException($"AI service error: {response.StatusCode} - {content}");
            }

            return JsonSerializer.Deserialize<AiWorkflowStateResponseDto>(content, _jsonOptions);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception when fetching AI workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<AiWorkflowStateResponseDto?> ResumeWorkflowAsync(
        string workflowId, 
        AiWorkflowResumeRequestDto resumeDto, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/internal/workflows/{workflowId}/resume", correlationId);
            var jsonPayload = JsonSerializer.Serialize(resumeDto, _jsonOptions);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation("Resuming AI workflow {WorkflowId} with action {Action}", workflowId, resumeDto.Action);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to resume AI workflow {WorkflowId}. Status: {StatusCode}, Response: {Content}", 
                    workflowId, response.StatusCode, content);
                throw new HttpRequestException($"AI service error: {response.StatusCode} - {content}");
            }

            return JsonSerializer.Deserialize<AiWorkflowStateResponseDto>(content, _jsonOptions);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception when resuming AI workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<AiWorkflowStateResponseDto?> CancelWorkflowAsync(
        string workflowId, 
        AiWorkflowCancelRequestDto cancelDto, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/internal/workflows/{workflowId}/cancel", correlationId);
            var jsonPayload = JsonSerializer.Serialize(cancelDto, _jsonOptions);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation("Cancelling AI workflow {WorkflowId}. Reason: {Reason}", workflowId, cancelDto.Reason);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to cancel AI workflow {WorkflowId}. Status: {StatusCode}, Response: {Content}", 
                    workflowId, response.StatusCode, content);
                throw new HttpRequestException($"AI service error: {response.StatusCode} - {content}");
            }

            return JsonSerializer.Deserialize<AiWorkflowStateResponseDto>(content, _jsonOptions);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception when cancelling AI workflow {WorkflowId}", workflowId);
            throw;
        }
    }
}
