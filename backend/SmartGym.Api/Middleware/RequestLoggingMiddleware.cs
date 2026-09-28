using System.Diagnostics;

namespace SmartGym.Api.Middleware;

/// <summary>
/// Structured request logging middleware tracking HTTP method, path, response status, and duration.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0}ms",
                method, path, statusCode, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
