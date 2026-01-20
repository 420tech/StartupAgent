namespace StartupAgent.Shared.Models;

/// <summary>
/// Recovery email sent to founder after session drop-off
/// </summary>
public class RecoveryEmail
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The drop-off event that triggered this email
    /// </summary>
    public required string SessionDropOffId { get; set; }

    /// <summary>
    /// Founder who receives this email
    /// </summary>
    public required string FounderId { get; set; }

    /// <summary>
    /// Recipient email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Resume link to continue session/assessment
    /// </summary>
    public required string ResumeLink { get; set; }

    /// <summary>
    /// Status of email send
    /// </summary>
    public RecoveryEmailSendStatus Status { get; set; } = RecoveryEmailSendStatus.Pending;

    /// <summary>
    /// Attempt count
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Last error if send failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When email was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When email was last attempted/sent
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When email was successfully sent
    /// </summary>
    public DateTime? SentAt { get; set; }
}

/// <summary>
/// Status of recovery email send
/// </summary>
public enum RecoveryEmailSendStatus
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
