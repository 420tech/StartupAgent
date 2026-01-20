using System.Net;
using System.Text.Json;
using StartupAgent.Shared.Models;

namespace StartupAgent.Modules.Shared.Middleware;

/// <summary>
/// Middleware to handle exceptions and return RFC 7807 problem+json responses.
/// Includes correlation IDs for log correlation.
/// </summary>
public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle non-2xx responses without body
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted)
            {
                await WriteProblemDetails(
                    context,
                    context.Response.StatusCode,
                    GetTitle(context.Response.StatusCode),
                    null
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var correlationId = context.GetCorrelationId() ?? "N/A";

            var problemDetails = new ProblemDetailsDto
            {
                Type = "https://httpstatuscodes.com/500",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred while processing the request.",
                TraceId = correlationId
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsJsonAsync(problemDetails, options);
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string? detail
    )
    {
        var correlationId = context.GetCorrelationId() ?? "N/A";

        var problemDetails = new ProblemDetailsDto
        {
            Type = $"https://httpstatuscodes.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            TraceId = correlationId
        };

        context.Response.ContentType = "application/problem+json";
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
            StatusCodes.Status429TooManyRequests => "Too Many Requests",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
            _ => $"HTTP {statusCode}"
        };
    }
}

/// <summary>
/// Extension methods for problem details middleware registration.
/// </summary>
public static class ProblemDetailsMiddlewareExtensions
{
    public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ProblemDetailsMiddleware>();
    }
}

/// <summary>
/// Exception types for common API errors.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string ProblemType { get; }
    public string ProblemTitle { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    public ApiException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        string? problemType = null,
        string? problemTitle = null,
        Dictionary<string, string[]>? validationErrors = null
    ) : base(message)
    {
        StatusCode = statusCode;
        ProblemType = problemType ?? $"https://httpstatuscodes.com/{statusCode}";
        ProblemTitle = problemTitle ?? "API Error";
        ValidationErrors = validationErrors;
    }
}

/// <summary>
/// Validation error exception (422 Unprocessable Entity).
/// </summary>
public class ValidationException : ApiException
{
    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(
            message,
            StatusCodes.Status422UnprocessableEntity,
            "https://httpstatuscodes.com/422",
            "Validation Error",
            errors
        )
    {
    }
}

/// <summary>
/// Authentication error exception (401 Unauthorized).
/// </summary>
public class AuthenticationException : ApiException
{
    public AuthenticationException(string message = "Authentication failed")
        : base(
            message,
            StatusCodes.Status401Unauthorized,
            "https://httpstatuscodes.com/401",
            "Unauthorized"
        )
    {
    }
}

/// <summary>
/// Authorization error exception (403 Forbidden).
/// </summary>
public class AuthorizationException : ApiException
{
    public AuthorizationException(string message = "Access denied")
        : base(
            message,
            StatusCodes.Status403Forbidden,
            "https://httpstatuscodes.com/403",
            "Forbidden"
        )
    {
    }
}

/// <summary>
/// Not found error exception (404 Not Found).
/// </summary>
public class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(
            message,
            StatusCodes.Status404NotFound,
            "https://httpstatuscodes.com/404",
            "Not Found"
        )
    {
    }
}
