namespace StartupAgent.Shared.Models;

/// <summary>
/// Tracks session drop-off events for recovery email triggering
/// </summary>
public class SessionDropOff
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The session that dropped off
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// The founder whose session dropped off
    /// </summary>
    public required string FounderId { get; set; }

    /// <summary>
    /// Assessment ID if the session was in progress
    /// </summary>
    public Guid? AssessmentId { get; set; }

    /// <summary>
    /// The last activity timestamp before drop-off
    /// </summary>
    public required DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Drop-off reason/trigger
    /// </summary>
    public required SessionDropOffReason Reason { get; set; }

    /// <summary>
    /// Status of the recovery process
    /// </summary>
    public RecoveryEmailStatus Status { get; set; } = RecoveryEmailStatus.Pending;

    /// <summary>
    /// When this drop-off was recorded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last status update
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Retry count for recovery email
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Error message if recovery failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Reason for session drop-off
/// </summary>
public enum SessionDropOffReason
{
    /// <summary>
    /// Session timeout (no activity for 30+ minutes)
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// Browser/tab closed
    /// </summary>
    BrowserClosed = 2,

    /// <summary>
    /// User manually left session
    /// </summary>
    UserLeft = 3,

    /// <summary>
    /// Network error or disconnect
    /// </summary>
    NetworkError = 4,

    /// <summary>
    /// Session inactive for 15+ minutes during diagnostic
    /// </summary>
    InactivityDuringDiagnostic = 5,

    /// <summary>
    /// Founder manually paused the session
    /// </summary>
    ManualPause = 6,

    /// <summary>
    /// Founder explicitly abandoned the diagnostic
    /// </summary>
    ExplicitAbandon = 7
}

/// <summary>
/// Status of recovery email process
/// </summary>
public enum RecoveryEmailStatus
{
    /// <summary>
    /// Waiting to be processed
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Recovery email is being sent
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Email sent successfully
    /// </summary>
    Sent = 3,

    /// <summary>
    /// Email send failed
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Marked as no longer needed (user resumed)
    /// </summary>
    Cancelled = 5
}
