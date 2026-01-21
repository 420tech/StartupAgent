using System.Text.Json;
using StartupAgent.Data.Repositories;
using StartupAgent.Shared.Contracts;
using StartupAgent.Shared.Models;

namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// Service interface for managing diagnostic sessions.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Start a new diagnostic session for a founder.
    /// </summary>
    Task<SessionDto> StartSessionAsync(string founderId, StartSessionDto dto);

    /// <summary>
    /// Get the current session for a founder (active or most recent).
    /// </summary>
    Task<SessionDto?> GetCurrentSessionAsync(string founderId);

    /// <summary>
    /// Get a specific session by ID.
    /// </summary>
    Task<SessionDto?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Get the next question in the session.
    /// </summary>
    Task<QuestionDto?> GetNextQuestionAsync(string sessionId);

    /// <summary>
    /// Submit an answer to the current question and advance session.
    /// </summary>
    Task<QuestionDto?> SubmitAnswerAsync(string sessionId, SubmitAnswerDto dto);

    /// <summary>
    /// Complete a session and generate assessment results.
    /// </summary>
    Task<SessionResultsDto> CompleteSessionAsync(string sessionId);

    /// <summary>
    /// Resume an incomplete session.
    /// </summary>
    Task<SessionDto?> ResumeSessionAsync(string sessionId);

    /// <summary>
    /// Auto-save an answer with optimistic concurrency control.
    /// </summary>
    Task<AutoSaveResultDto> AutoSaveAnswerAsync(string sessionId, AutoSaveAnswerDto dto);
}

/// <summary>
/// Implementation of session management service.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IQuestionBankService _questionBankService;
    private readonly IMindsetDetectionService _mindsetDetectionService;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ISessionRepository sessionRepository,
        IAssessmentRepository assessmentRepository,
        IQuestionBankService questionBankService,
        IMindsetDetectionService mindsetDetectionService,
        ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _assessmentRepository = assessmentRepository;
        _questionBankService = questionBankService;
        _mindsetDetectionService = mindsetDetectionService;
        _logger = logger;
    }

    public async Task<SessionDto> StartSessionAsync(string founderId, StartSessionDto dto)
    {
        // Check for existing active session
        var activeSessions = await _sessionRepository.GetActiveSessionsByFounderAsync(founderId);
        if (activeSessions.Any())
        {
            _logger.LogWarning("Founder {FounderId} already has an active session", founderId);
            throw new InvalidOperationException("Founder already has an active session");
        }

        var session = new Session
        {
            Id = Guid.NewGuid().ToString(),
            FounderId = founderId,
            ProgressState = "mindset_opener",
            Status = SessionStatus.Active,
            AnswersJson = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // If mindset answer provided, detect mindset
        if (!string.IsNullOrEmpty(dto.MindsetAnswer))
        {
            session.DetectedMindset = _mindsetDetectionService.DetectMindsetFromAnswer(dto.MindsetAnswer);
            var answers = new Dictionary<string, string> { { "mindset_opener", dto.MindsetAnswer } };
            session.AnswersJson = JsonSerializer.Serialize(answers);
            session.ProgressState = "q1"; // Move to first real question
        }

        await _sessionRepository.AddAsync(session);
        await _sessionRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Session started for founder {FounderId}, SessionId: {SessionId}, Mindset: {Mindset}",
            founderId, session.Id, session.DetectedMindset);

        return MapToDto(session);
    }

    public async Task<SessionDto?> GetCurrentSessionAsync(string founderId)
    {
        var session = await _sessionRepository.GetMostRecentSessionAsync(founderId);
        return session == null ? null : MapToDto(session);
    }

    public async Task<SessionDto?> GetSessionAsync(string sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        return session == null ? null : MapToDto(session);
    }

    public async Task<QuestionDto?> GetNextQuestionAsync(string sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return null;
        }

        if (session.Status != SessionStatus.Active)
        {
            _logger.LogWarning("Session {SessionId} is not active", sessionId);
            return null;
        }

        // Parse current answers to get count
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(session.AnswersJson) ?? new();
        var questionCount = answers.Count(kvp => kvp.Key != "mindset_opener");

        // Get next question
        var currentQuestionId = session.ProgressState;
        var nextQuestion = _questionBankService.GetNextQuestion(
            currentQuestionId,
            answers.ContainsKey(currentQuestionId) ? answers[currentQuestionId] : "",
            session.DetectedMindset,
            questionCount);

        return nextQuestion;
    }

    public async Task<QuestionDto?> SubmitAnswerAsync(string sessionId, SubmitAnswerDto dto)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return null;
        }

        if (session.Status != SessionStatus.Active)
        {
            _logger.LogWarning("Cannot submit answer to inactive session: {SessionId}", sessionId);
            return null;
        }

        // Parse and update answers
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(session.AnswersJson) ?? new();
        answers[session.ProgressState] = dto.Answer;

        // Refine mindset based on answer
        if (session.DetectedMindset.HasValue)
        {
            session.DetectedMindset = _mindsetDetectionService.RefineMindsetDetection(
                session.DetectedMindset.Value,
                session.ProgressState,
                dto.Answer);
        }

        session.AnswersJson = JsonSerializer.Serialize(answers);
        session.UpdatedAt = DateTime.UtcNow;

        // Get next question
        var questionCount = answers.Count(kvp => kvp.Key != "mindset_opener");
        var nextQuestion = _questionBankService.GetNextQuestion(
            session.ProgressState,
            dto.Answer,
            session.DetectedMindset,
            questionCount);

        if (nextQuestion == null)
        {
            // No more questions - mark session as complete
            session.Status = SessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.ProgressState = "completed";

            _logger.LogInformation("Session completed: {SessionId}", sessionId);
        }
        else
        {
            // Update progress state to next question
            session.ProgressState = nextQuestion.Id;
        }

        await _sessionRepository.UpdateAsync(session);
        await _sessionRepository.SaveChangesAsync();

        _logger.LogDebug(
            "Answer submitted for session {SessionId}, question {QuestionId}. Next question: {NextQuestionId}",
            sessionId, session.ProgressState, nextQuestion?.Id);

        return nextQuestion;
    }

    public async Task<SessionResultsDto> CompleteSessionAsync(string sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        if (session.Status != SessionStatus.Completed)
        {
            _logger.LogWarning("Session {SessionId} is not completed", sessionId);
            throw new InvalidOperationException($"Session {sessionId} is not completed");
        }

        // Create assessment from session data
        var assessment = new Assessment
        {
            Id = Guid.NewGuid().ToString(),
            FounderId = session.FounderId,
            OverallScore = 65, // TODO: Calculate from answers
            DimensionScoresJson = "{}",
            RoadmapText = "Your personalized roadmap will appear here.", // TODO: Generate from answers
            RiskBriefText = "Investor risk brief will appear here.", // TODO: Generate from answers
            DetectedMindset = session.DetectedMindset,
            Status = ReportStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        await _assessmentRepository.AddAsync(assessment);
        await _assessmentRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Assessment created for session {SessionId}, AssessmentId: {AssessmentId}",
            sessionId, assessment.Id);

        return new SessionResultsDto
        {
            SessionId = sessionId,
            OverallScore = assessment.OverallScore,
            DimensionScores = new Dictionary<string, int>(),
            DetectedMindset = session.DetectedMindset?.ToString() ?? "Unknown",
            ReadinessStatus = "Yellow", // TODO: Calculate from score
            RoadmapText = assessment.RoadmapText,
            RiskBriefText = assessment.RiskBriefText
        };
    }

    public async Task<SessionDto?> ResumeSessionAsync(string sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        if (session.Status != SessionStatus.Active)
        {
            _logger.LogWarning("Cannot resume non-active session: {SessionId}", sessionId);
            return null;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);
        await _sessionRepository.SaveChangesAsync();

        _logger.LogInformation("Session resumed: {SessionId}", sessionId);

        return MapToDto(session);
    }

    public async Task<AutoSaveResultDto> AutoSaveAnswerAsync(string sessionId, AutoSaveAnswerDto dto)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
            {
                return new AutoSaveResultDto
                {
                    SessionId = sessionId,
                    Success = false,
                    Message = "Session not found"
                };
            }

            if (session.Status != SessionStatus.Active)
            {
                return new AutoSaveResultDto
                {
                    SessionId = sessionId,
                    Success = false,
                    Message = "Session is not active"
                };
            }

            // Parse current answers
            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(session.AnswersJson) ?? new();

            // Update answer
            answers[dto.QuestionId] = dto.Answer;
            session.AnswersJson = JsonSerializer.Serialize(answers);
            session.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(session);
            await _sessionRepository.SaveChangesAsync();

            _logger.LogDebug(
                "Answer auto-saved for session {SessionId}, question {QuestionId}",
                sessionId, dto.QuestionId);

            return new AutoSaveResultDto
            {
                SessionId = sessionId,
                RowVersion = session.RowVersion,
                SavedAt = session.UpdatedAt,
                Success = true,
                Message = "Answer auto-saved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-saving answer for session {SessionId}", sessionId);
            return new AutoSaveResultDto
            {
                SessionId = sessionId,
                Success = false,
                Message = "Error saving answer"
            };
        }
    }

    private static SessionDto MapToDto(Session session)
    {
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(session.AnswersJson) ?? new();
        var totalAnswers = answers.Count(kvp => kvp.Key != "mindset_opener");

        return new SessionDto
        {
            Id = session.Id,
            FounderId = session.FounderId,
            ProgressState = session.ProgressState,
            DetectedMindset = session.DetectedMindset?.ToString(),
            Status = session.Status.ToString(),
            ProgressPercentage = Math.Min((totalAnswers * 100) / 15, 100),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            CompletedAt = session.CompletedAt
        };
    }
}
