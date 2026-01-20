using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartupAgent.Modules.Shared.Services;
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
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISessionService sessionService,
        IAssessmentService assessmentService,
        ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _assessmentService = assessmentService;
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
}
