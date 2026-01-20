using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StartupAgent.Data;
using StartupAgent.Shared.Models;
using StartupAgent.Server.Services.Jobs;
using Microsoft.EntityFrameworkCore;

namespace StartupAgent.Server.Modules.Events.Controllers;

[ApiController]
[Route("api/v1/events")]
[Authorize]
public class SessionDropOffController(
    ApplicationDbContext context,
    IRecoveryEmailJobQueue recoveryEmailJobQueue,
    ILogger<SessionDropOffController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IRecoveryEmailJobQueue _recoveryEmailJobQueue = recoveryEmailJobQueue;
    private readonly ILogger<SessionDropOffController> _logger = logger;

    /// <summary>
    /// Log a session drop-off event and queue recovery email
    /// </summary>
    /// <remarks>
    /// Called when a founder's session becomes inactive or ends prematurely.
    /// Triggers recovery email workflow.
    /// </remarks>
    [HttpPost("session-drop-off")]
    public async Task<IActionResult> LogSessionDropOff(
        [FromBody] SessionDropOffPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload == null)
        {
            return BadRequest(new { error = "Missing session drop-off payload" });
        }

        try
        {
            // Get founder ID from JWT claim
            var founderIdClaim = User.FindFirst("FounderId");
            if (founderIdClaim == null)
            {
                return Unauthorized(new { error = "Founder ID not found in token" });
            }

            var founderId = founderIdClaim.Value;

            // Validate founder exists
            var founder = await _context.Founders
                .FirstOrDefaultAsync(f => f.Id == founderId, cancellationToken);

            if (founder == null)
            {
                return NotFound(new { error = "Founder not found" });
            }

            // Parse drop-off reason
            if (!Enum.TryParse<SessionDropOffReason>(payload.Reason, ignoreCase: true, out var reason))
            {
                return BadRequest(new { error = $"Invalid drop-off reason: {payload.Reason}" });
            }

            _logger.LogInformation(
                "Session drop-off recorded for founder {FounderId}: reason={Reason}, sessionId={SessionId}",
                founderId,
                reason,
                payload.SessionId);

            // Create session drop-off event
            var dropOff = new SessionDropOff
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = payload.SessionId,
                FounderId = founderId,
                AssessmentId = string.IsNullOrEmpty(payload.AssessmentId) ? null : Guid.Parse(payload.AssessmentId),
                LastActivityAt = payload.LastActivityAt ?? DateTime.UtcNow,
                Reason = reason,
                Status = RecoveryEmailStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SessionDropOffs.Add(dropOff);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Session drop-off event {DropOffId} created", dropOff.Id);

            // Create recovery email record
            var resumeLink = BuildResumeLink(founderId, payload.SessionId);
            var recoveryEmail = new RecoveryEmail
            {
                Id = Guid.NewGuid().ToString(),
                SessionDropOffId = dropOff.Id,
                FounderId = founderId,
                Email = founder.Email,
                ResumeLink = resumeLink,
                Status = RecoveryEmailSendStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RecoveryEmails.Add(recoveryEmail);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Recovery email {RecoveryEmailId} created for founder {FounderId}",
                recoveryEmail.Id,
                founderId);

            // Queue recovery email for sending (will send within 2h per acceptance criteria)
            await _recoveryEmailJobQueue.QueueJobAsync(recoveryEmail.Id, cancellationToken);

            _logger.LogInformation(
                "Recovery email {RecoveryEmailId} queued for sending",
                recoveryEmail.Id);

            return Accepted(new
            {
                dropOffId = dropOff.Id,
                recoveryEmailId = recoveryEmail.Id,
                status = "Recovery email queued",
                message = "Session drop-off recorded and recovery email queued"
            });
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid GUID format in session drop-off payload");
            return BadRequest(new { error = "Invalid assessment ID format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording session drop-off: {Error}", ex.Message);
            return StatusCode(500, new { error = "Failed to record session drop-off" });
        }
    }

    /// <summary>
    /// Get session drop-off status
    /// </summary>
    [HttpGet("session-drop-off/{dropOffId}")]
    public async Task<IActionResult> GetDropOffStatus(
        string dropOffId,
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

            var dropOff = await _context.SessionDropOffs
                .FirstOrDefaultAsync(d => d.Id == dropOffId && d.FounderId == founderId, cancellationToken);

            if (dropOff == null)
            {
                return NotFound(new { error = "Drop-off event not found" });
            }

            var recoveryEmail = await _context.RecoveryEmails
                .FirstOrDefaultAsync(r => r.SessionDropOffId == dropOffId, cancellationToken);

            return Ok(new
            {
                dropOffId = dropOff.Id,
                reason = dropOff.Reason.ToString(),
                status = dropOff.Status.ToString(),
                createdAt = dropOff.CreatedAt,
                recoveryEmailStatus = recoveryEmail?.Status.ToString(),
                recoveryEmailSentAt = recoveryEmail?.SentAt,
                recoveryEmailError = recoveryEmail?.LastError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drop-off status: {Error}", ex.Message);
            return StatusCode(500, new { error = "Failed to retrieve drop-off status" });
        }
    }

    /// <summary>
    /// Build resume link for recovery email
    /// </summary>
    private string BuildResumeLink(string founderId, string sessionId)
    {
        // Construct resume link to diagnostic assessment
        // Format: https://app.startupaigent.com/resume?sessionId={sessionId}&token={founder-token}
        var baseUrl = Request.Scheme + "://" + Request.Host;
        return $"{baseUrl}/resume?sessionId={Uri.EscapeDataString(sessionId)}&founderId={Uri.EscapeDataString(founderId)}";
    }
}

/// <summary>
/// Payload for session drop-off event
/// </summary>
public class SessionDropOffPayload
{
    /// <summary>
    /// Session ID that dropped off
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Assessment ID if in progress (optional)
    /// </summary>
    public string? AssessmentId { get; set; }

    /// <summary>
    /// Reason for drop-off (Timeout, BrowserClosed, UserLeft, NetworkError)
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime? LastActivityAt { get; set; }
}
