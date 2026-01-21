using Microsoft.EntityFrameworkCore;
using StartupAgent.Data;
using StartupAgent.Shared.Models;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Service interface for session drop-off detection and management.
/// </summary>
public interface ISessionDropOffService
{
    /// <summary>
    /// Detect and record inactive sessions (>15 minutes without activity).
    /// </summary>
    Task DetectInactiveSessions(CancellationToken cancellationToken);

    /// <summary>
    /// Manually pause a session and record the drop-off.
    /// </summary>
    Task PauseSession(string sessionId, string founderId, string? comments = null);

    /// <summary>
    /// Mark a session as abandoned by founder.
    /// </summary>
    Task AbandonSession(string sessionId, string founderId, string? comments = null);

    /// <summary>
    /// Check drop-off status for a session.
    /// </summary>
    Task<SessionDropOff?> GetDropOffStatus(string sessionId);
}

/// <summary>
/// Implementation of session drop-off detection and management.
/// </summary>
public class SessionDropOffService : ISessionDropOffService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionDropOffService> _logger;
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromMinutes(15);

    public SessionDropOffService(
        ApplicationDbContext context,
        ILogger<SessionDropOffService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Detect and record sessions inactive for >15 minutes.
    /// Runs periodically (every 5 minutes) to identify drop-offs during diagnostic.
    /// </summary>
    public async Task DetectInactiveSessions(CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(InactivityThreshold);

        try
        {
            // Find active sessions that haven't been updated in >15 minutes
            var inactiveSessions = await _context.Sessions
                .Where(s => s.Status == SessionStatus.Active && s.UpdatedAt < cutoffTime)
                .ToListAsync(cancellationToken);

            if (inactiveSessions.Count == 0)
            {
                _logger.LogDebug("No inactive sessions detected");
                return;
            }

            foreach (var session in inactiveSessions)
            {
                // Create drop-off record
                var dropOff = new SessionDropOff
                {
                    Id = Guid.NewGuid().ToString(),
                    SessionId = session.Id,
                    FounderId = session.FounderId,
                    LastActivityAt = session.UpdatedAt,
                    Reason = SessionDropOffReason.InactivityDuringDiagnostic,
                    Status = RecoveryEmailStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SessionDropOffs.Add(dropOff);

                // Pause the session
                session.Status = SessionStatus.Paused;
                session.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Detected inactive session: {SessionId} for founder {FounderId}. " +
                    "Last activity: {LastActivity}",
                    session.Id,
                    session.FounderId,
                    session.UpdatedAt);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SessionDropOffService detected and paused {Count} inactive sessions",
                inactiveSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during inactivity detection");
        }
    }

    /// <summary>
    /// Manually pause a session when founder clicks "Save & Exit" button.
    /// </summary>
    public async Task PauseSession(string sessionId, string founderId, string? comments = null)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(new object[] { sessionId }, cancellationToken: default);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            if (session.FounderId != founderId)
            {
                _logger.LogWarning(
                    "Founder {FounderId} attempted to pause session {SessionId} belonging to {OwnerFounderId}",
                    founderId, sessionId, session.FounderId);
                throw new InvalidOperationException("Unauthorized");
            }

            // Create drop-off record
            var dropOff = new SessionDropOff
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                FounderId = session.FounderId,
                LastActivityAt = DateTime.UtcNow,
                Reason = SessionDropOffReason.ManualPause,
                Status = RecoveryEmailStatus.Cancelled, // Don't send recovery email for manual pause
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SessionDropOffs.Add(dropOff);

            // Pause the session
            session.Status = SessionStatus.Paused;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Session paused: {SessionId} for founder {FounderId}.",
                session.Id,
                session.FounderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Mark a session as abandoned when founder explicitly gives up.
    /// </summary>
    public async Task AbandonSession(string sessionId, string founderId, string? comments = null)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(new object[] { sessionId }, cancellationToken: default);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            if (session.FounderId != founderId)
            {
                _logger.LogWarning(
                    "Founder {FounderId} attempted to abandon session {SessionId} belonging to {OwnerFounderId}",
                    founderId, sessionId, session.FounderId);
                throw new InvalidOperationException("Unauthorized");
            }

            // Create drop-off record
            var dropOff = new SessionDropOff
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                FounderId = session.FounderId,
                LastActivityAt = DateTime.UtcNow,
                Reason = SessionDropOffReason.ExplicitAbandon,
                Status = RecoveryEmailStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ErrorMessage = comments
            };

            _context.SessionDropOffs.Add(dropOff);

            // Mark as abandoned
            session.Status = SessionStatus.Abandoned;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Session abandoned: {SessionId} for founder {FounderId}. Reason: {Comments}",
                session.Id,
                session.FounderId,
                comments ?? "No reason provided");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Get drop-off status for a session.
    /// </summary>
    public async Task<SessionDropOff?> GetDropOffStatus(string sessionId)
    {
        return await _context.SessionDropOffs
            .Where(d => d.SessionId == sessionId)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
