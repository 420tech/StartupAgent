namespace StartupAgent.Shared.Models.Scoring;

/// <summary>
/// Complete results summary for a diagnostic session
/// </summary>
public class DiagnosticResults
{
    public required string SessionId { get; set; }

    /// <summary>
    /// Overall readiness score (0-100)
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Overall status: "Green", "Yellow", "Red"
    /// </summary>
    public required string OverallStatus { get; set; }

    /// <summary>
    /// All dimension scores (typically 9 dimensions per the framework)
    /// </summary>
    public List<DimensionScore> DimensionScores { get; set; } = [];

    /// <summary>
    /// Founder's detected mindset bucket
    /// </summary>
    public required string MindsetBucket { get; set; }

    /// <summary>
    /// When results were generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Narrative/guidance text tailored to founder's mindset
    /// </summary>
    public string? Narrative { get; set; }

    /// <summary>
    /// AI confidence in the overall assessment (0-100)
    /// </summary>
    public int OverallConfidence { get; set; }

    /// <summary>
    /// Top 3 priority areas to focus on (dimension names)
    /// </summary>
    public List<string> TopPriorities { get; set; } = [];

    /// <summary>
    /// Number of questions answered in the diagnostic
    /// </summary>
    public int QuestionsAnswered { get; set; }
}
