namespace StartupAgent.Shared.Models;

/// <summary>
/// Diagnostic session representing an in-progress or completed assessment.
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier for the session (primary key).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the founder.
    /// </summary>
    public string FounderId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to founder.
    /// </summary>
    public Founder? Founder { get; set; }

    /// <summary>
    /// Current progress state (e.g., "Q5", "completed").
    /// </summary>
    public string ProgressState { get; set; } = string.Empty;

    /// <summary>
    /// Detected founder mindset (overwhelmed, stuck, confident-but-unsure, pre-fundraise).
    /// </summary>
    public MindsetType? DetectedMindset { get; set; }

    /// <summary>
    /// JSON serialized object containing all diagnostic answers.
    /// Structure: { "question_id": "answer_text", ... }
    /// </summary>
    public string AnswersJson { get; set; } = "{}";

    /// <summary>
    /// Session status tracking.
    /// </summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>
    /// Timestamp when session was created (started).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when session was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when session was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
