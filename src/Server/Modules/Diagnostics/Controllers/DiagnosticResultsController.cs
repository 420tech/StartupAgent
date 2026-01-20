using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartupAgent.Shared.Models.Scoring;
using StartupAgent.Shared.Services.Scoring;
using StartupAgent.Shared.Services.Pdf;

namespace StartupAgent.Server.Modules.Diagnostics.Controllers;

[ApiController]
[Route("api/v1/diagnostics")]
[Authorize]
public class DiagnosticResultsController(
    IScoringService scoringService,
    IRoadmapPdfService pdfService) : ControllerBase
{
    private readonly IScoringService _scoringService = scoringService;
    private readonly IRoadmapPdfService _pdfService = pdfService;
    /// <summary>
    /// Get results for a completed diagnostic session
    /// </summary>
    [HttpGet("{sessionId}/results")]
    public async Task<ActionResult<DiagnosticResults>> GetResultsAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Fetch session and answers from database
            // TODO: Validate user owns this session via RLS
            // TODO: Check if diagnostic is complete
            
            // Placeholder - would fetch from DB
            var results = new DiagnosticResults
            {
                SessionId = sessionId,
                OverallScore = 0,
                OverallStatus = "Pending",
                MindsetBucket = "unknown",
                GeneratedAt = DateTime.UtcNow,
                OverallConfidence = 0,
                QuestionsAnswered = 0
            };

            return Ok(results);
        }
        catch
        {
            // Log error
            return StatusCode(500, new { error = "Failed to retrieve results" });
        }
    }

    /// <summary>
    /// Download 90-day roadmap as PDF
    /// </summary>
    [HttpGet("{sessionId}/roadmap-pdf")]
    public async Task<IActionResult> DownloadRoadmapPdfAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Fetch session and results from database
            // TODO: Validate user owns this session via RLS
            
            // Placeholder - would fetch from DB and use actual results
            var results = new DiagnosticResults
            {
                SessionId = sessionId,
                OverallScore = 68,
                OverallStatus = "Yellow",
                MindsetBucket = "confident-but-unsure",
                GeneratedAt = DateTime.UtcNow,
                OverallConfidence = 85,
                QuestionsAnswered = 15,
                Narrative = "You've got solid fundamentals and you're asking the right questions. Your foundation is stronger than you think.",
                TopPriorities = new List<string>
                {
                    "Fix governance gap (Red → Yellow in 2 weeks)",
                    "Clarify unit economics (Yellow → Green in 1 week)",
                    "Strengthen GTM strategy (Yellow → Green in 4 weeks)"
                },
                DimensionScores = new List<DimensionScore>
                {
                    new DimensionScore
                    {
                        DimensionName = "Problem Validation",
                        Score = 75,
                        Status = "Green",
                        ConfidenceLevel = 90,
                        ConfidenceNote = "Strong user research with 20+ interviews",
                        RisksAndOpportunities = new List<string> { "Strong user research foundation" }
                    },
                    new DimensionScore
                    {
                        DimensionName = "Traction",
                        Score = 55,
                        Status = "Yellow",
                        ConfidenceLevel = 75,
                        ConfidenceNote = "Early signups but engagement needs improvement",
                        RisksAndOpportunities = new List<string> { "Early signups but low engagement" }
                    }
                }
            };

            var pdfBytes = await _pdfService.GeneratePdfAsync(results, cancellationToken);
            
            var fileName = $"StartupAgent-Roadmap-{sessionId}-{DateTime.UtcNow:yyyyMMdd}.pdf";
            
            // Log successful PDF generation
            // TODO: Add telemetry with correlation ID
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch
        {
            // Log error with correlation ID
            // TODO: Add structured logging with correlation ID
            
            return StatusCode(500, new 
            { 
                error = "Failed to generate PDF. Please try again.",
                canRetry = true,
                correlationId = Guid.NewGuid().ToString()
            });
        }
    }
}
