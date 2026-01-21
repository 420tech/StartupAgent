using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartupAgent.Modules.Shared.Services;
using StartupAgent.Server.Services.Jobs;
using StartupAgent.Shared.Contracts;

namespace StartupAgent.Controllers;

/// <summary>
/// API endpoints for diagnostic session management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IAssessmentService _assessmentService;
    private readonly ISessionDropOffService _dropOffService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISessionService sessionService,
        IAssessmentService assessmentService,
        ISessionDropOffService dropOffService,
        ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _assessmentService = assessmentService;
        _dropOffService = dropOffService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new diagnostic session.
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionDto>> StartSession([FromBody] StartSessionDto dto)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            _logger.LogWarning("User without NameIdentifier claim attempted to start session");
            return Unauthorized();
        }

        try
        {
            var session = await _sessionService.StartSessionAsync(founderId, dto);
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to start session for founder {FounderId}", founderId);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get the current active session for the authenticated founder.
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> GetCurrentSession()
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetCurrentSessionAsync(founderId);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    /// <summary>
    /// Get a specific session by ID (founder can only view their own sessions).
    /// </summary>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SessionDto>> GetSession(string sessionId)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        // Verify founder owns this session
        if (session.FounderId != founderId)
        {
            _logger.LogWarning(
                "Founder {FounderId} attempted to access session {SessionId} belonging to {OwnerFounderId}",
                founderId, sessionId, session.FounderId);
            return Forbid();
        }

        return Ok(session);
    }

    /// <summary>
    /// Get the next question in the session.
    /// </summary>
    [HttpGet("{sessionId}/next-question")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionDto>> GetNextQuestion(string sessionId)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        var question = await _sessionService.GetNextQuestionAsync(sessionId);
        if (question == null)
        {
            return BadRequest(new { message = "No more questions available or session not active" });
        }

        return Ok(question);
    }

    /// <summary>
    /// Submit an answer to the current question.
    /// </summary>
    [HttpPost("{sessionId}/answer")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionDto>> SubmitAnswer(string sessionId, [FromBody] SubmitAnswerDto dto)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(dto.Answer))
        {
            return BadRequest(new { message = "Answer cannot be empty" });
        }

        var nextQuestion = await _sessionService.SubmitAnswerAsync(sessionId, dto);

        // If session is now complete, return completion status
        var updatedSession = await _sessionService.GetSessionAsync(sessionId);
        if (updatedSession?.Status == "Completed")
        {
            _logger.LogInformation(
                "Session {SessionId} for founder {FounderId} completed",
                sessionId, founderId);

            // Redirect client to results endpoint
            return Ok(new
            {
                message = "Session completed",
                sessionId = sessionId,
                redirectTo = $"/api/v1/session/{sessionId}/results"
            });
        }

        return Ok(nextQuestion);
    }

    /// <summary>
    /// Get session results after completion.
    /// </summary>
    [HttpGet("{sessionId}/results")]
    [ProducesResponseType(typeof(SessionResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessionResultsDto>> GetSessionResults(string sessionId)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        if (session.Status != "Completed")
        {
            return BadRequest(new { message = "Session is not completed yet" });
        }

        try
        {
            var results = await _sessionService.CompleteSessionAsync(sessionId);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get results for session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Resume an incomplete session.
    /// </summary>
    [HttpPost("{sessionId}/resume")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SessionDto>> ResumeSession(string sessionId)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        var resumedSession = await _sessionService.ResumeSessionAsync(sessionId);
        if (resumedSession == null)
        {
            return BadRequest(new { message = "Failed to resume session" });
        }

        return Ok(resumedSession);
    }

    /// <summary>
    /// Auto-save current answer (optimistic concurrency with rowversion).
    /// Saves answer within 2 seconds with conflict detection.
    /// </summary>
    [HttpPost("{sessionId}/auto-save")]
    [ProducesResponseType(typeof(AutoSaveResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AutoSaveResultDto>> AutoSaveAnswer(
        string sessionId,
        [FromBody] AutoSaveAnswerDto dto,
        CancellationToken cancellationToken)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(dto.Answer))
        {
            return BadRequest(new { message = "Answer cannot be empty" });
        }

        try
        {
            // Auto-save with optimistic concurrency
            var result = await _sessionService.AutoSaveAnswerAsync(sessionId, dto);

            _logger.LogInformation(
                "Auto-saved answer for session {SessionId}, question {QuestionId}",
                sessionId,
                dto.QuestionId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Auto-save failed for session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Pause a session and record drop-off event for recovery.
    /// Founder explicitly clicks "Save & Exit" or similar action.
    /// </summary>
    [HttpPost("{sessionId}/pause")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PauseSession(
        string sessionId,
        [FromBody] object? payload = null)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        try
        {
            await _dropOffService.PauseSession(sessionId, founderId, "Manual pause via UI");

            _logger.LogInformation(
                "Session paused by founder: {SessionId}",
                sessionId);

            return Ok(new { message = "Session paused. You can resume anytime." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get drop-off status for a session.
    /// Returns information about when/why session was paused or abandoned.
    /// </summary>
    [HttpGet("{sessionId}/drop-off-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDropOffStatus(string sessionId)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        var dropOff = await _dropOffService.GetDropOffStatus(sessionId);
        if (dropOff == null)
        {
            return NotFound(new { message = "No drop-off recorded for this session" });
        }

        return Ok(new
        {
            sessionId = dropOff.SessionId,
            reason = dropOff.Reason.ToString(),
            lastActivityAt = dropOff.LastActivityAt,
            detectedAt = dropOff.CreatedAt
        });
    }

    /// <summary>
    /// Mark a session as abandoned by founder.
    /// Used when founder explicitly gives up on the diagnostic.
    /// </summary>
    [HttpPost("{sessionId}/abandon")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AbandonSession(
        string sessionId,
        [FromBody] AbandonSessionDto? dto = null)
    {
        var founderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(founderId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.FounderId != founderId)
        {
            return Forbid();
        }

        try
        {
            var reason = dto?.Reason ?? "Founder abandoned diagnostic";
            await _dropOffService.AbandonSession(sessionId, founderId, reason);

            _logger.LogInformation(
                "Session abandoned by founder: {SessionId}. Reason: {Reason}",
                sessionId,
                reason);

            return Ok(new
            {
                message = "Session abandoned. Feel free to reach out if you'd like guidance—no pressure.",
                sessionId = sessionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to abandon session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
    }
}
