namespace StartupAgent.Shared.Models.Scoring;

/// <summary>
/// Represents a single dimension's assessment result with score, status, and confidence.
/// </summary>
public class DimensionScore
{
    /// <summary>
    /// The dimension being assessed (e.g., "Traction", "GTM", "Team Strength")
    /// </summary>
    public required string DimensionName { get; set; }

    /// <summary>
    /// Numeric score 0-100
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Traffic light status: "Green" (75-100), "Yellow" (50-74), "Red" (0-49)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Confidence level in this assessment (0-100) based on answer quality and clarity
    /// </summary>
    public int ConfidenceLevel { get; set; }

    /// <summary>
    /// Human-readable confidence note explaining the assessment
    /// </summary>
    public string? ConfidenceNote { get; set; }

    /// <summary>
    /// Key evidence excerpts from founder's answers supporting this score
    /// </summary>
    public List<string> EvidenceExcerpts { get; set; } = [];

    /// <summary>
    /// Highlighted risks or opportunities for this dimension
    /// </summary>
    public List<string> RisksAndOpportunities { get; set; } = [];

    /// <summary>
    /// Weight of this dimension in overall scoring (e.g., 0.15 for 15%)
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Weighted contribution to overall score
    /// </summary>
    public decimal WeightedScore => Score * Weight;
}
