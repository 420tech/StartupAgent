using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartupAgent.Data;
using StartupAgent.Server.Services.Storage;

namespace StartupAgent.Server.Modules.Decks.Controllers;

[ApiController]
[Route("api/v1/decks")]
[Authorize(Policy = "ValidFounder")]
public class DeckUploadController(
    IDeckUploadService uploadService,
    ApplicationDbContext context) : ControllerBase
{
    private readonly IDeckUploadService _uploadService = uploadService;
    private readonly ApplicationDbContext _context = context;

    /// <summary>
    /// Upload a pitch deck for analysis
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52428800)] // 50 MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> UploadDeck(
        [FromForm] IFormFile file,
        [FromForm] string assessmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get founder ID from auth claims
            var founderIdClaim = User.FindFirst("FounderId");
            if (founderIdClaim == null)
            {
                return Unauthorized(new { error = "Founder ID not found in token" });
            }

            var founderId = founderIdClaim.Value;

            // Validate assessment exists and belongs to founder
            var assessment = await _context.Assessments
                .FindAsync(new object[] { Guid.Parse(assessmentId) }, cancellationToken);

            if (assessment == null)
            {
                return NotFound(new { error = "Assessment not found" });
            }

            if (assessment.FounderId != founderId)
            {
                return Forbid();
            }

            // Upload the file
            var uploadResult = await _uploadService.UploadDeckAsync(
                file,
                founderId,
                assessmentId,
                cancellationToken);

            if (!uploadResult.Success)
            {
                return BadRequest(new
                {
                    error = uploadResult.ErrorMessage
                });
            }

            // Update DeckAnalysis record (create if doesn't exist)
            var deckAnalysis = await _context.DeckAnalyses
                .FirstOrDefaultAsync(d => d.AssessmentId == assessment.Id, cancellationToken);

            if (deckAnalysis == null)
            {
                // Create new DeckAnalysis record
                deckAnalysis = new StartupAgent.Shared.Models.DeckAnalysis
                {
                    Id = Guid.NewGuid().ToString(),
                    AssessmentId = assessment.Id,
                    FileUrl = uploadResult.FilePath!,
                    FileName = uploadResult.FileName!,
                    OriginalFileName = uploadResult.OriginalFileName,
                    FileSizeBytes = uploadResult.FileSizeBytes,
                    Status = StartupAgent.Shared.Models.ReportStatus.Pending,
                    InsightsJson = "{}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.DeckAnalyses.Add(deckAnalysis);
            }
            else
            {
                // Update existing record with new file
                // Delete old file first
                if (!string.IsNullOrEmpty(deckAnalysis.FileUrl))
                {
                    await _uploadService.DeleteDeckAsync(deckAnalysis.FileUrl, cancellationToken);
                }

                deckAnalysis.FileUrl = uploadResult.FilePath!;
                deckAnalysis.FileName = uploadResult.FileName!;
                deckAnalysis.OriginalFileName = uploadResult.OriginalFileName;
                deckAnalysis.FileSizeBytes = uploadResult.FileSizeBytes;
                deckAnalysis.Status = StartupAgent.Shared.Models.ReportStatus.Pending;
                deckAnalysis.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"Deck upload completed for assessment {assessmentId}: {uploadResult.FileName}");

            return Ok(new
            {
                message = "Deck uploaded successfully",
                deckAnalysisId = deckAnalysis.Id,
                fileName = uploadResult.FileName,
                originalFileName = uploadResult.OriginalFileName,
                fileSizeBytes = uploadResult.FileSizeBytes,
                uploadedAt = uploadResult.UploadedAt
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in deck upload endpoint: {ex.Message}");
            return StatusCode(500, new
            {
                error = "Upload failed. Please try again.",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get deck upload status for an assessment
    /// </summary>
    [HttpGet("status/{assessmentId}")]
    public async Task<IActionResult> GetDeckStatus(
        string assessmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var founderIdClaim = User.FindFirst("FounderId");
            if (founderIdClaim == null)
            {
                return Unauthorized(new { error = "Founder ID not found in token" });
            }

            var founderId = founderIdClaim.Value;

            var assessment = await _context.Assessments
                .FindAsync(new object[] { Guid.Parse(assessmentId) }, cancellationToken);

            if (assessment == null || assessment.FounderId != founderId)
            {
                return NotFound(new { error = "Assessment not found" });
            }

            var deckAnalysis = await _context.DeckAnalyses
                .FirstOrDefaultAsync(d => d.AssessmentId == assessment.Id, cancellationToken);

            if (deckAnalysis == null)
            {
                return Ok(new
                {
                    hasUpload = false,
                    status = "None"
                });
            }

            return Ok(new
            {
                hasUpload = true,
                deckAnalysisId = deckAnalysis.Id,
                fileName = deckAnalysis.FileName,
                originalFileName = deckAnalysis.OriginalFileName,
                fileSizeBytes = deckAnalysis.FileSizeBytes,
                status = deckAnalysis.Status,
                uploadedAt = deckAnalysis.CreatedAt,
                updatedAt = deckAnalysis.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting deck status: {ex.Message}");
            return StatusCode(500, new { error = "Failed to retrieve deck status" });
        }
    }
}
