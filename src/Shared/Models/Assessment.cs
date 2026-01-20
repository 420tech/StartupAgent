namespace StartupAgent.Shared.Models;

/// <summary>
/// Assessment result from completed diagnostic.
/// </summary>
public class Assessment
{
    /// <summary>
    /// Unique identifier for the assessment (primary key).
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
    /// Overall readiness score (0-100).
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// JSON serialized dimension scores.
    /// Structure: { "dimension_name": score_value, ... }
    /// Includes all 9 dimensions: problem_validation, user_research, mvp_quality, traction, go_to_market, revenue_model, operations_legal, team_strength, runway
    /// </summary>
    public string DimensionScoresJson { get; set; } = "{}";

    /// <summary>
    /// Founder-facing roadmap with priorities and action plan.
    /// </summary>
    public string RoadmapText { get; set; } = string.Empty;

    /// <summary>
    /// Investor-focused risk brief with risk indices.
    /// </summary>
    public string RiskBriefText { get; set; } = string.Empty;

    /// <summary>
    /// Detected founder mindset at time of assessment.
    /// </summary>
    public MindsetType? DetectedMindset { get; set; }

    /// <summary>
    /// Overall readiness status based on score.
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// Timestamp when assessment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when assessment results were generated.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property to any associated deck analysis.
    /// </summary>
    public DeckAnalysis? DeckAnalysis { get; set; }
}
