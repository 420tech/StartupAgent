namespace StartupAgent.Shared.Contracts;

/// <summary>
/// Represents a diagnostic question in the questionnaire.
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Unique question identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Question text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Dimension this question assesses.
    /// </summary>
    public string Dimension { get; set; } = string.Empty;

    /// <summary>
    /// Question type (text, multiple-choice, scale, yes-no).
    /// </summary>
    public string QuestionType { get; set; } = "text";

    /// <summary>
    /// Optional answer options for multiple-choice questions.
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Progress indicator (current question number).
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Total questions in this session.
    /// </summary>
    public int TotalQuestions { get; set; }
}

/// <summary>
/// Request DTO for submitting an answer to a question.
/// </summary>
public class SubmitAnswerDto
{
    /// <summary>
    /// The answer provided by the founder.
    /// Can be text, selected option, or scale value.
    /// </summary>
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for auto-saving an answer (with rowversion for optimistic concurrency).
/// </summary>
public class AutoSaveAnswerDto
{
    /// <summary>
    /// The question ID being answered.
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// The answer being saved.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Rowversion from client for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Response DTO for auto-save result.
/// </summary>
public class AutoSaveResultDto
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Updated rowversion from server.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Timestamp of save.
    /// </summary>
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Whether save was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Message if any.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Response DTO for session information.
/// </summary>
public class SessionDto
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Founder ID.
    /// </summary>
    public string FounderId { get; set; } = string.Empty;

    /// <summary>
    /// Current progress state.
    /// </summary>
    public string ProgressState { get; set; } = string.Empty;

    /// <summary>
    /// Detected mindset (if available).
    /// </summary>
    public string? DetectedMindset { get; set; }

    /// <summary>
    /// Session status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// When session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When session was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When session was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Request DTO for starting a new diagnostic session.
/// </summary>
public class StartSessionDto
{
    /// <summary>
    /// Optional opening mindset question answer.
    /// </summary>
    public string? MindsetAnswer { get; set; }
}

/// <summary>
/// Response DTO for session results.
/// </summary>
public class SessionResultsDto
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Overall readiness score (0-100).
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Dimension scores as JSON.
    /// </summary>
    public Dictionary<string, int> DimensionScores { get; set; } = new();

    /// <summary>
    /// Detected founder mindset.
    /// </summary>
    public string DetectedMindset { get; set; } = string.Empty;

    /// <summary>
    /// Traffic light status: Red, Yellow, Green.
    /// </summary>
    public string ReadinessStatus { get; set; } = string.Empty;

    /// <summary>
    /// Roadmap text for founder.
    /// </summary>
    public string RoadmapText { get; set; } = string.Empty;

    /// <summary>
    /// Risk brief for investors.
    /// </summary>
    public string RiskBriefText { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for abandoning a session.
/// </summary>
public class AbandonSessionDto
{
    /// <summary>
    /// Reason for abandonment (optional feedback).
    /// </summary>
    public string? Reason { get; set; }
}
