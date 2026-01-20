using StartupAgent.Shared.Models;

namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// Service for detecting founder mindset from diagnostic responses.
/// </summary>
public interface IMindsetDetectionService
{
    /// <summary>
    /// Detect founder mindset from opening question answer.
    /// </summary>
    MindsetType DetectMindsetFromAnswer(string answer);

    /// <summary>
    /// Refine mindset detection based on subsequent answers.
    /// </summary>
    MindsetType RefineMindsetDetection(MindsetType currentMindset, string questionId, string answer);
}

/// <summary>
/// Implementation of mindset detection service.
/// MVP version uses keyword matching; future versions can use LLM for nuanced analysis.
/// </summary>
public class MindsetDetectionService : IMindsetDetectionService
{
    private readonly ILogger<MindsetDetectionService> _logger;

    public MindsetDetectionService(ILogger<MindsetDetectionService> logger)
    {
        _logger = logger;
    }

    public MindsetType DetectMindsetFromAnswer(string answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return MindsetType.ConfidentButUnsure; // Default mindset
        }

        var lowerAnswer = answer.ToLower();

        // Check for overwhelmed indicators
        if (lowerAnswer.Contains("overwhelmed") || lowerAnswer.Contains("too many") || lowerAnswer.Contains("don't know where"))
        {
            _logger.LogDebug("Detected Overwhelmed mindset");
            return MindsetType.Overwhelmed;
        }

        // Check for stuck indicators
        if (lowerAnswer.Contains("stuck") || lowerAnswer.Contains("wrong") || lowerAnswer.Contains("can't figure") ||
            lowerAnswer.Contains("blocked") || lowerAnswer.Contains("stalled"))
        {
            _logger.LogDebug("Detected Stuck mindset");
            return MindsetType.Stuck;
        }

        // Check for confident-but-unsure indicators
        if (lowerAnswer.Contains("confident") || lowerAnswer.Contains("feeling good") || 
            lowerAnswer.Contains("validate") || lowerAnswer.Contains("right things"))
        {
            _logger.LogDebug("Detected Confident-but-unsure mindset");
            return MindsetType.ConfidentButUnsure;
        }

        // Check for pre-fundraise indicators
        if (lowerAnswer.Contains("fundraise") || lowerAnswer.Contains("raise capital") || 
            lowerAnswer.Contains("investors") || lowerAnswer.Contains("readiness"))
        {
            _logger.LogDebug("Detected Pre-fundraise mindset");
            return MindsetType.PreFundraise;
        }

        // Default to confident-but-unsure if no strong indicators
        _logger.LogDebug("No strong mindset indicators, defaulting to ConfidentButUnsure");
        return MindsetType.ConfidentButUnsure;
    }

    public MindsetType RefineMindsetDetection(MindsetType currentMindset, string questionId, string answer)
    {
        // MVP: Keep initial mindset detection
        // Future: Use multiple answers to refine or confirm mindset
        // Could use sentiment analysis, tone detection, or LLM-based analysis
        
        _logger.LogDebug(
            "Refining mindset detection for question {QuestionId}. Current mindset: {CurrentMindset}",
            questionId, currentMindset);

        return currentMindset;
    }
}
