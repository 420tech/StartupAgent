namespace StartupAgent.Shared.Models;

/// <summary>
/// Problem details error response (RFC 7807).
/// </summary>
public class ProblemDetailsDto
{
    /// <summary>
    /// URI reference identifying problem type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Short human-readable summary.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Explanation specific to this occurrence.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Trace ID for correlation with server logs.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Validation errors (for 422 responses).
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Supported user mindsets for adaptive questioning.
/// </summary>
public enum MindsetType
{
    FirstTimer,
    SerialEntrepreneur,
    BusinessSide,
    Confident
}

/// <summary>
/// Diagnostic session status tracking.
/// </summary>
public enum SessionStatus
{
    /// <summary>User is actively completing the diagnostic.</summary>
    Active,

    /// <summary>Session paused, can be resumed.</summary>
    Paused,

    /// <summary>Diagnostic completed and results generated.</summary>
    Completed,

    /// <summary>Session abandoned (24h auto-cleanup).</summary>
    Abandoned
}

/// <summary>
/// Report generation status.
/// </summary>
public enum ReportStatus
{
    /// <summary>Report generation queued.</summary>
    Pending,

    /// <summary>Report currently being generated.</summary>
    Running,

    /// <summary>Report generation succeeded.</summary>
    Succeeded,

    /// <summary>Report generation failed.</summary>
    Failed,

    /// <summary>Report generation in retry.</summary>
    Retrying,

    /// <summary>Report generation timed out.</summary>
    Timeout
}
