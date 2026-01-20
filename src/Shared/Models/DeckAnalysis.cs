namespace StartupAgent.Shared.Models;

/// <summary>
/// Optional pitch deck analysis results.
/// </summary>
public class DeckAnalysis
{
    /// <summary>
    /// Unique identifier for the deck analysis (primary key).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the assessment.
    /// </summary>
    public string AssessmentId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to assessment.
    /// </summary>
    public Assessment? Assessment { get; set; }

    /// <summary>
    /// Uploaded file URL/path in blob storage.
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// File name as uploaded by user.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized AI insights from pitch deck analysis.
    /// Structure: { 
    ///   "tam_clarity": "...", 
    ///   "gtm_clarity": "...", 
    ///   "metrics_consistency": "...",
    ///   "governance_signals": "..."
    /// }
    /// </summary>
    public string InsightsJson { get; set; } = "{}";

    /// <summary>
    /// Analysis status (pending, running, succeeded, failed, retrying, timeout).
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when analysis was requested.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when analysis completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }
}
