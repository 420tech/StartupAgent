using System.Text.Json;
using StartupAgent.Data.Repositories;
using StartupAgent.Shared.Models;

namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// Service for generating assessments from session data.
/// </summary>
public interface IAssessmentService
{
    /// <summary>
    /// Generate an assessment from session answers.
    /// </summary>
    Task<Assessment> GenerateAssessmentAsync(Session session);

    /// <summary>
    /// Calculate scores for each 9-dimension category.
    /// </summary>
    Dictionary<string, int> CalculateDimensionScores(Dictionary<string, string> answers);

    /// <summary>
    /// Determine readiness status (Red/Yellow/Green) based on overall score.
    /// </summary>
    string DetermineReadinessStatus(int overallScore);

    /// <summary>
    /// Generate personalized roadmap text based on weak dimensions.
    /// </summary>
    string GenerateRoadmapText(Dictionary<string, int> dimensionScores, string? mindset);

    /// <summary>
    /// Generate risk brief for investor consideration.
    /// </summary>
    string GenerateRiskBrief(Dictionary<string, int> dimensionScores, string? mindset);
}

/// <summary>
/// Implementation of assessment service.
/// </summary>
public class AssessmentService : IAssessmentService
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ILogger<AssessmentService> _logger;

    // Dimension weighting - revenue-focused dimensions weighted higher
    private static readonly Dictionary<string, int> DimensionWeights = new()
    {
        { "problem_validation", 10 },
        { "user_research", 10 },
        { "mvp_quality", 12 },
        { "traction", 15 },
        { "go_to_market", 15 },
        { "revenue_model", 15 },
        { "operations_legal", 10 },
        { "team", 12 },
        { "runway", 1 } // Lower weight on runway, focus on product/market fit
    };

    public AssessmentService(IAssessmentRepository assessmentRepository, ILogger<AssessmentService> logger)
    {
        _assessmentRepository = assessmentRepository;
        _logger = logger;
    }

    public async Task<Assessment> GenerateAssessmentAsync(Session session)
    {
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(session.AnswersJson) ?? new();
        var dimensionScores = CalculateDimensionScores(answers);
        var overallScore = CalculateOverallScore(dimensionScores);
        var readinessStatus = DetermineReadinessStatus(overallScore);
        var roadmapText = GenerateRoadmapText(dimensionScores, session.DetectedMindset?.ToString());
        var riskBrief = GenerateRiskBrief(dimensionScores, session.DetectedMindset?.ToString());

        var assessment = new Assessment
        {
            Id = Guid.NewGuid().ToString(),
            FounderId = session.FounderId,
            OverallScore = overallScore,
            DimensionScoresJson = JsonSerializer.Serialize(dimensionScores),
            RoadmapText = roadmapText,
            RiskBriefText = riskBrief,
            DetectedMindset = session.DetectedMindset,
            Status = ReportStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        await _assessmentRepository.AddAsync(assessment);
        await _assessmentRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Assessment generated for founder {FounderId}, score: {OverallScore}, status: {Status}",
            session.FounderId, overallScore, readinessStatus);

        return assessment;
    }

    public Dictionary<string, int> CalculateDimensionScores(Dictionary<string, string> answers)
    {
        var scores = new Dictionary<string, int>
        {
            { "problem_validation", 0 },
            { "user_research", 0 },
            { "mvp_quality", 0 },
            { "traction", 0 },
            { "go_to_market", 0 },
            { "revenue_model", 0 },
            { "operations_legal", 0 },
            { "team", 0 },
            { "runway", 0 }
        };

        // Simple scoring logic - answer quality/completeness
        // In MVP, we score based on answer length and presence of key positive indicators
        foreach (var kvp in answers)
        {
            if (kvp.Key == "mindset_opener")
                continue;

            var dimension = GetDimensionForQuestion(kvp.Key);
            if (dimension != null && scores.ContainsKey(dimension))
            {
                var score = ScoreAnswer(kvp.Value, dimension);
                scores[dimension] = (scores[dimension] + score) / 2; // Average if multiple answers per dimension
            }
        }

        return scores;
    }

    public string DetermineReadinessStatus(int overallScore)
    {
        return overallScore switch
        {
            >= 75 => "Green",
            >= 50 => "Yellow",
            _ => "Red"
        };
    }

    public string GenerateRoadmapText(Dictionary<string, int> dimensionScores, string? mindset)
    {
        var weakDimensions = dimensionScores
            .OrderBy(d => d.Value)
            .Where(d => d.Value < 60)
            .Select(d => d.Key)
            .ToList();

        if (!weakDimensions.Any())
        {
            return "Your startup is well-positioned across all dimensions! Focus on maintaining momentum and scaling efficiently.";
        }

        var roadmap = "### Recommended Roadmap\n\n";
        roadmap += "Based on your diagnostic session, prioritize:\n\n";

        var priorityMap = new Dictionary<string, string>
        {
            { "problem_validation", "**1. Validate the problem**: Conduct deeper user interviews to ensure the problem you're solving is real and significant." },
            { "user_research", "**1. Expand user research**: Talk to more potential customers to understand their needs and preferences." },
            { "mvp_quality", "**2. Improve MVP**: Focus on product quality and ensuring it delivers clear value." },
            { "traction", "**2. Build traction**: Implement growth strategies and actively acquire users." },
            { "go_to_market", "**3. GTM strategy**: Develop a clear go-to-market strategy with defined channels." },
            { "revenue_model", "**3. Revenue model**: Test and validate your pricing and revenue model." },
            { "operations_legal", "**4. Ops & legal**: Handle cap table, governance, and legal requirements." },
            { "team", "**4. Team building**: Identify skill gaps and recruit key talent." },
            { "runway", "**5. Secure funding**: Consider fundraising to extend runway." }
        };

        foreach (var weak in weakDimensions.Take(3))
        {
            if (priorityMap.TryGetValue(weak, out var guidance))
            {
                roadmap += guidance + "\n\n";
            }
        }

        if (mindset == "Overwhelmed")
        {
            roadmap += "\n**Note**: You mentioned feeling overwhelmed. Consider breaking these into smaller, weekly milestones to reduce stress.";
        }

        return roadmap;
    }

    public string GenerateRiskBrief(Dictionary<string, int> dimensionScores, string? mindset)
    {
        var avgScore = dimensionScores.Values.Average();
        var risks = new List<string>();

        if (dimensionScores["problem_validation"] < 60)
            risks.Add("Problem validation is incomplete; risk of building for the wrong market.");

        if (dimensionScores["user_research"] < 60)
            risks.Add("Limited user research; target market understanding is unclear.");

        if (dimensionScores["mvp_quality"] < 60)
            risks.Add("MVP may not be ready for launch; product-market fit uncertain.");

        if (dimensionScores["traction"] < 40)
            risks.Add("Early stage; no significant traction yet.");

        if (dimensionScores["revenue_model"] < 60)
            risks.Add("Revenue model not validated; business model risk is high.");

        if (dimensionScores["team"] < 50)
            risks.Add("Team gaps identified; execution risk is elevated.");

        if (dimensionScores["runway"] < 40)
            risks.Add("Limited runway; urgent need to reach key milestones.");

        var riskText = "### Investor Risk Assessment\n\n";
        riskText += $"**Overall Stage**: {(avgScore < 40 ? "Pre-seed" : avgScore < 60 ? "Seed" : "Series A Ready")}\n\n";

        if (risks.Any())
        {
            riskText += "**Key Risks**:\n";
            foreach (var risk in risks)
            {
                riskText += $"- {risk}\n";
            }
        }
        else
        {
            riskText += "**Key Risks**: Minimal - startup appears well-positioned for growth.\n";
        }

        riskText += $"\n**Mindset**: Founder detected as {mindset ?? "Unknown"}. ";
        riskText += mindset switch
        {
            "Overwhelmed" => "Consider support resources to improve execution confidence.",
            "Stuck" => "Recommend strategic mentorship to overcome current blockers.",
            "PreFundraise" => "Well-prepared for investor conversations; timing is good.",
            _ => "Maintain momentum and keep validating assumptions."
        };

        return riskText;
    }

    private int CalculateOverallScore(Dictionary<string, int> dimensionScores)
    {
        var totalWeight = 0;
        var weightedScore = 0;

        foreach (var dimension in dimensionScores)
        {
            if (DimensionWeights.TryGetValue(dimension.Key, out var weight))
            {
                weightedScore += dimension.Value * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0 ? weightedScore / totalWeight : 0;
    }

    private string? GetDimensionForQuestion(string questionId)
    {
        // Map question ID to dimension
        return questionId switch
        {
            "pv_1" or "pv_2" => "problem_validation",
            "ur_1" or "ur_2" => "user_research",
            "mvp_1" or "mvp_2" => "mvp_quality",
            "tr_1" or "tr_2" => "traction",
            "gtm_1" or "gtm_2" => "go_to_market",
            "rev_1" or "rev_2" => "revenue_model",
            "ops_1" or "ops_2" => "operations_legal",
            "team_1" or "team_2" => "team",
            "runway_1" => "runway",
            _ => null
        };
    }

    private int ScoreAnswer(string? answer, string dimension)
    {
        // Basic scoring heuristic based on answer quality
        var answerLength = answer?.Length ?? 0;

        // Longer, more detailed answers score higher
        var lengthScore = Math.Min(answerLength / 50, 100); // Normalize to 0-100

        // Positive indicators boost score
        var hasPositiveIndicators = HasPositiveIndicators(answer, dimension);
        var indicatorBoost = hasPositiveIndicators ? 20 : 0;

        return Math.Min(lengthScore + indicatorBoost, 100);
    }

    private bool HasPositiveIndicators(string? answer, string dimension)
    {
        if (string.IsNullOrEmpty(answer))
            return false;

        var lowerAnswer = answer.ToLower();

        return dimension switch
        {
            "problem_validation" => lowerAnswer.Contains("customer") || lowerAnswer.Contains("validate") || lowerAnswer.Contains("pain"),
            "user_research" => lowerAnswer.Contains("interview") || lowerAnswer.Contains("research") || lowerAnswer.Contains("talk"),
            "mvp_quality" => lowerAnswer.Contains("beta") || lowerAnswer.Contains("launch") || lowerAnswer.Contains("ready"),
            "traction" => lowerAnswer.Contains("user") || lowerAnswer.Contains("growth") || lowerAnswer.Contains("retention"),
            "go_to_market" => lowerAnswer.Contains("channel") || lowerAnswer.Contains("acquisition") || lowerAnswer.Contains("market"),
            "revenue_model" => lowerAnswer.Contains("paying") || lowerAnswer.Contains("revenue") || lowerAnswer.Contains("customer"),
            "operations_legal" => lowerAnswer.Contains("cap table") || lowerAnswer.Contains("legal") || lowerAnswer.Contains("complian"),
            "team" => lowerAnswer.Contains("engineer") || lowerAnswer.Contains("founder") || lowerAnswer.Contains("team"),
            "runway" => lowerAnswer.Contains("month") || lowerAnswer.Contains("fund") || lowerAnswer.Contains("cash"),
            _ => false
        };
    }
}
