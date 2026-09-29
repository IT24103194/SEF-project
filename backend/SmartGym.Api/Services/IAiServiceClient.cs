using SmartGym.Api.DTOs.AI;

namespace SmartGym.Api.Services;

/// <summary>
/// Client contract for secure backend-to-AI microservice communication.
/// Handles internal authentication, correlation tracking, and workflow orchestration.
/// </summary>
public interface IAiServiceClient
{
    Task<AiHealthResponseDto?> CheckHealthAsync(CancellationToken cancellationToken = default);

    Task<AiWorkflowStateResponseDto?> StartWorkflowAsync(
        AiWorkflowStartRequestDto request, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default);

    Task<AiWorkflowStateResponseDto?> GetWorkflowAsync(
        string workflowId, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default);

    Task<AiWorkflowStateResponseDto?> ResumeWorkflowAsync(
        string workflowId, 
        AiWorkflowResumeRequestDto request, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default);

    Task<AiWorkflowStateResponseDto?> CancelWorkflowAsync(
        string workflowId, 
        AiWorkflowCancelRequestDto request, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default);
}
