namespace StartupAgent.Server.Services.Analysis;

/// <summary>
/// Service for analyzing pitch decks and extracting insights
/// </summary>
public interface IDeckAnalysisService
{
    /// <summary>
    /// Analyze a pitch deck file and extract insights
    /// </summary>
    Task<DeckInsights> AnalyzeDeckAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

public class DeckAnalysisService : IDeckAnalysisService
{
    private const int AnalysisTimeoutSeconds = 300; // 5 minutes

    public async Task<DeckInsights> AnalyzeDeckAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Starting deck analysis for: {filePath}");

        // Create timeout token
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(AnalysisTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Deck file not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            Console.WriteLine($"Analyzing deck: {fileInfo.Name} ({fileInfo.Length} bytes)");

            // TODO: Integrate with AI/LLM service (Azure OpenAI, OpenAI, etc.)
            // For now, simulate analysis with delay
            await Task.Delay(2000, linkedCts.Token);

            // Generate mock insights (placeholder for real AI analysis)
            var insights = GenerateMockInsights(fileInfo.Name);

            Console.WriteLine($"Deck analysis completed for: {filePath}");
            return insights;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            Console.Error.WriteLine($"Deck analysis timed out after {AnalysisTimeoutSeconds} seconds: {filePath}");
            throw new TimeoutException($"Deck analysis exceeded timeout of {AnalysisTimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error analyzing deck {filePath}: {ex.Message}");
            throw;
        }
    }

    private DeckInsights GenerateMockInsights(string fileName)
    {
        // TODO: Replace with real AI-extracted insights
        return new DeckInsights
        {
            TamClarity = "Market size clearly articulated with $2.5B TAM estimate. Breakdown by segment provided but lacks validation sources.",
            GtmClarity = "Go-to-market strategy focuses on B2B SaaS sales with PLG motion. Clear customer acquisition funnel but missing CAC/LTV metrics.",
            MetricsConsistency = "Revenue projections show 3x YoY growth. Some inconsistency between slide 8 (user metrics) and slide 12 (revenue model). Monthly cohort retention mentioned but not visualized.",
            GovernanceSignals = "Strong founding team with relevant domain expertise. Board composition mentioned but advisor details limited. No mention of existing investor rights or cap table structure.",
            StorytellingScore = 8,
            VisualQualityScore = 7,
            DataRigorScore = 6,
            RedFlags = new List<string>
            {
                "Revenue projections appear aggressive without supporting validation",
                "Competitive landscape slide mentions 'no direct competitors' which may signal weak market research"
            },
            Strengths = new List<string>
            {
                "Clear problem-solution fit with customer testimonials",
                "Strong technical differentiation with proprietary IP",
                "Experienced founding team with prior exits"
            },
            RecommendedFollowUp = new List<string>
            {
                "Request detailed financial model with assumptions breakdown",
                "Verify customer testimonials and reference calls",
                "Deep dive on competitive positioning and market validation"
            },
            AnalyzedAt = DateTime.UtcNow,
            AnalysisVersion = "1.0-mock"
        };
    }
}

/// <summary>
/// Insights extracted from pitch deck analysis
/// </summary>
public class DeckInsights
{
    /// <summary>
    /// Assessment of Total Addressable Market clarity
    /// </summary>
    public string TamClarity { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of Go-To-Market strategy clarity
    /// </summary>
    public string GtmClarity { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of metrics consistency across deck
    /// </summary>
    public string MetricsConsistency { get; set; } = string.Empty;

    /// <summary>
    /// Governance and team signals
    /// </summary>
    public string GovernanceSignals { get; set; } = string.Empty;

    /// <summary>
    /// Storytelling quality score (1-10)
    /// </summary>
    public int StorytellingScore { get; set; }

    /// <summary>
    /// Visual quality score (1-10)
    /// </summary>
    public int VisualQualityScore { get; set; }

    /// <summary>
    /// Data rigor score (1-10)
    /// </summary>
    public int DataRigorScore { get; set; }

    /// <summary>
    /// Red flags identified
    /// </summary>
    public List<string> RedFlags { get; set; } = new();

    /// <summary>
    /// Strengths identified
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Recommended follow-up questions
    /// </summary>
    public List<string> RecommendedFollowUp { get; set; } = new();

    /// <summary>
    /// When analysis was performed
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// Analysis engine version
    /// </summary>
    public string AnalysisVersion { get; set; } = string.Empty;
}
