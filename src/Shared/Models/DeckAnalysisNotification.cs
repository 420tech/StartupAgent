namespace StartupAgent.Shared.Models;

/// <summary>
/// Deck analysis notification sent to founder after analysis completes
/// </summary>
public class DeckAnalysisNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The deck analysis that completed/failed
    /// </summary>
    public required string DeckAnalysisId { get; set; }

    /// <summary>
    /// Founder who receives this notification
    /// </summary>
    public required string FounderId { get; set; }

    /// <summary>
    /// Recipient email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Type of notification (Success or Failure)
    /// </summary>
    public required DeckAnalysisNotificationType NotificationType { get; set; }

    /// <summary>
    /// Status of notification send
    /// </summary>
    public DeckAnalysisNotificationStatus Status { get; set; } = DeckAnalysisNotificationStatus.Pending;

    /// <summary>
    /// Attempt count
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Last error if send failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When notification was last attempted/sent
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When notification was successfully sent
    /// </summary>
    public DateTime? SentAt { get; set; }
}

/// <summary>
/// Type of deck analysis notification
/// </summary>
public enum DeckAnalysisNotificationType
{
    /// <summary>
    /// Analysis completed successfully
    /// </summary>
    Success = 1,

    /// <summary>
    /// Analysis failed
    /// </summary>
    Failure = 2
}

/// <summary>
/// Status of deck analysis notification
/// </summary>
public enum DeckAnalysisNotificationStatus
{
    /// <summary>
    /// Queued and waiting to send
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Currently sending
    /// </summary>
    Sending = 2,

    /// <summary>
    /// Successfully sent
    /// </summary>
    Sent = 3,

    /// <summary>
    /// Failed permanently after retries
    /// </summary>
    Failed = 4
}
