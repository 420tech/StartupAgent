using StartupAgent.Shared.Models.Scoring;

namespace StartupAgent.Shared.Services.Scoring;

/// <summary>
/// Service to compute diagnostic results and scores from session answers
/// </summary>
public interface IScoringService
{
    /// <summary>
    /// Calculate scores and results for a completed diagnostic session
    /// </summary>
    Task<DiagnosticResults> CalculateResultsAsync(
        string sessionId,
        Dictionary<string, string> answers,
        string mindsetBucket,
        CancellationToken cancellationToken = default);
}

public class ScoringService : IScoringService
{
    // Dimension definitions with weights per TB Software Readiness Framework™
    private static readonly Dictionary<string, (string DisplayName, decimal Weight)> DimensionWeights = new()
    {
        { "problem_validation", ("Problem Validation", 0.12m) },
        { "user_research", ("User Research", 0.10m) },
        { "mvp_quality", ("MVP Quality", 0.10m) },
        { "traction", ("Traction", 0.25m) },
        { "go_to_market", ("Go-to-Market", 0.15m) },
        { "revenue_model", ("Revenue Model", 0.08m) },
        { "operations_legal", ("Operations & Legal", 0.12m) },
        { "team_strength", ("Team Strength", 0.05m) },
        { "runway", ("Runway", 0.03m) }
    };

    public async Task<DiagnosticResults> CalculateResultsAsync(
        string sessionId,
        Dictionary<string, string> answers,
        string mindsetBucket,
        CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - will be enhanced with scoring logic
        await Task.CompletedTask;

        var dimensionScores = new List<DimensionScore>();

        // Initialize all dimensions with placeholder scores
        foreach (var (key, (displayName, weight)) in DimensionWeights)
        {
            // Score calculation would go here - for now, generate placeholder
            var score = Random.Shared.Next(0, 100);
            var status = GetStatusFromScore(score);

            dimensionScores.Add(new DimensionScore
            {
                DimensionName = displayName,
                Score = score,
                Status = status,
                ConfidenceLevel = Random.Shared.Next(60, 100),
                ConfidenceNote = GenerateConfidenceNote(status),
                Weight = weight,
                EvidenceExcerpts = [],
                RisksAndOpportunities = []
            });
        }

        var overallScore = (int)dimensionScores.Sum(d => d.WeightedScore);
        var overallStatus = GetStatusFromScore(overallScore);

        // Identify top 3 priorities (yellow/red items)
        var topPriorities = dimensionScores
            .Where(d => d.Status != "Green")
            .OrderBy(d => d.Score)
            .Take(3)
            .Select(d => d.DimensionName)
            .ToList();

        return new DiagnosticResults
        {
            SessionId = sessionId,
            OverallScore = overallScore,
            OverallStatus = overallStatus,
            DimensionScores = dimensionScores,
            MindsetBucket = mindsetBucket,
            GeneratedAt = DateTime.UtcNow,
            OverallConfidence = Random.Shared.Next(70, 95),
            TopPriorities = topPriorities,
            QuestionsAnswered = answers.Count
        };
    }

    private static string GetStatusFromScore(int score) => score switch
    {
        >= 75 => "Green",
        >= 50 => "Yellow",
        _ => "Red"
    };

    private static string GenerateConfidenceNote(string status) => status switch
    {
        "Green" => "Strong assessment based on clear answers and positive indicators.",
        "Yellow" => "Moderate assessment - some areas need clarification or improvement.",
        "Red" => "Clear assessment - this area needs immediate attention.",
        _ => "Assessment confidence pending."
    };
}
