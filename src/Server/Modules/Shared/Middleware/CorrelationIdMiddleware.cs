using System.Diagnostics;

namespace StartupAgent.Modules.Shared.Middleware;

/// <summary>
/// Middleware to propagate correlation IDs across requests.
/// Ensures distributed tracing across client, API, jobs, and external calls.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string RequestIdHeader = "X-Request-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate correlation ID
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerCorrelationId)
            ? headerCorrelationId.ToString()
            : Guid.NewGuid().ToString();

        var requestId = Guid.NewGuid().ToString();

        // Store in HttpContext items for access throughout request pipeline
        context.Items[CorrelationIdHeader] = correlationId;
        context.Items[RequestIdHeader] = requestId;

        // Add to response headers for client to track
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers[RequestIdHeader] = requestId;

        // Add to Activity (OpenTelemetry) for distributed tracing
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.AddTag("http.request.correlation_id", correlationId);
            activity.AddTag("http.request.request_id", requestId);
        }

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { CorrelationIdHeader, correlationId },
            { RequestIdHeader, requestId }
        }))
        {
            _logger.LogInformation(
                "Request started: {Method} {Path} [CorrelationId: {CorrelationId}, RequestId: {RequestId}]",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                requestId
            );

            await _next(context);

            _logger.LogInformation(
                "Request completed: {StatusCode} [CorrelationId: {CorrelationId}]",
                context.Response.StatusCode,
                correlationId
            );
        }
    }
}

/// <summary>
/// Extension methods for correlation ID middleware registration.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Helper to extract correlation ID from HttpContext.
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items.TryGetValue("X-Correlation-Id", out var correlationId)
            ? correlationId?.ToString()
            : null;
    }

    /// <summary>
    /// Helper to extract request ID from HttpContext.
    /// </summary>
    public static string? GetRequestId(this HttpContext context)
    {
        return context.Items.TryGetValue("X-Request-Id", out var requestId)
            ? requestId?.ToString()
            : null;
    }
}
