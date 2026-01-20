using StartupAgent.Shared.Models;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository interface for Session entity with specialized queries.
/// </summary>
public interface ISessionRepository : IRepository<Session>
{
    /// <summary>
    /// Get active sessions for a founder.
    /// </summary>
    Task<IEnumerable<Session>> GetActiveSessionsByFounderAsync(string founderId);

    /// <summary>
    /// Get the most recent session for a founder.
    /// </summary>
    Task<Session?> GetMostRecentSessionAsync(string founderId);

    /// <summary>
    /// Get completed sessions for a founder.
    /// </summary>
    Task<IEnumerable<Session>> GetCompletedSessionsByFounderAsync(string founderId);
}
