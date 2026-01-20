using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StartupAgent.Data;
using StartupAgent.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace StartupAgent.Server.Modules.Decks.Controllers;

[ApiController]
[Route("api/v1/notifications/deck-analysis")]
[Authorize(Policy = "ValidFounder")]
public class DeckAnalysisNotificationController(
    ApplicationDbContext context,
    ILogger<DeckAnalysisNotificationController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<DeckAnalysisNotificationController> _logger = logger;

    /// <summary>
    /// Get deck analysis notification status
    /// </summary>
    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetNotificationStatus(
        string notificationId,
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

            var notification = await _context.DeckAnalysisNotifications
                .FirstOrDefaultAsync(
                    n => n.Id == notificationId && n.FounderId == founderId,
                    cancellationToken);

            if (notification == null)
            {
                return NotFound(new { error = "Notification not found" });
            }

            return Ok(new
            {
                notificationId = notification.Id,
                deckAnalysisId = notification.DeckAnalysisId,
                type = notification.NotificationType.ToString(),
                status = notification.Status.ToString(),
                sentAt = notification.SentAt,
                createdAt = notification.CreatedAt,
                attemptCount = notification.AttemptCount,
                lastError = notification.LastError,
                correlationId = notification.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification status: {Error}", ex.Message);
            return StatusCode(500, new { error = "Failed to retrieve notification status" });
        }
    }

    /// <summary>
    /// Get all notifications for a deck analysis
    /// </summary>
    [HttpGet("deck/{deckAnalysisId}")]
    public async Task<IActionResult> GetDeckNotifications(
        string deckAnalysisId,
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

            // Verify deck analysis belongs to founder
            var deckAnalysis = await _context.DeckAnalyses
                .Include(d => d.Assessment)
                .FirstOrDefaultAsync(
                    d => d.Id == deckAnalysisId && d.Assessment!.FounderId == founderId,
                    cancellationToken);

            if (deckAnalysis == null)
            {
                return NotFound(new { error = "Deck analysis not found" });
            }

            var notifications = await _context.DeckAnalysisNotifications
                .Where(n => n.DeckAnalysisId == deckAnalysisId && n.FounderId == founderId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    notificationId = n.Id,
                    type = n.NotificationType.ToString(),
                    status = n.Status.ToString(),
                    sentAt = n.SentAt,
                    createdAt = n.CreatedAt,
                    correlationId = n.CorrelationId
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                deckAnalysisId,
                notifications,
                count = notifications.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deck notifications: {Error}", ex.Message);
            return StatusCode(500, new { error = "Failed to retrieve notifications" });
        }
    }
}
