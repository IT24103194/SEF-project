namespace SmartGym.Api.Middleware;

/// <summary>
/// Correlation ID middleware ensuring every HTTP request carries a traceable correlation identifier
/// across logs, response headers, and downstream processing.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Resolve or generate correlation ID
        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var existingCorrelationId) 
            && !string.IsNullOrWhiteSpace(existingCorrelationId))
        {
            correlationId = existingCorrelationId.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        // 2. Store in HttpContext.Items for internal consumption
        context.Items[CorrelationIdItemKey] = correlationId;

        // 3. Attach to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            }
            return Task.CompletedTask;
        });

        // 4. Enrich structured logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdItemKey] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
