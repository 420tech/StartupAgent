using Microsoft.EntityFrameworkCore;
using StartupAgent.Shared.Models;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository implementation for Session entity.
/// </summary>
public class SessionRepository : Repository<Session>, ISessionRepository
{
    public SessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Session>> GetActiveSessionsByFounderAsync(string founderId)
    {
        return await _dbSet
            .Where(s => s.FounderId == founderId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Session?> GetMostRecentSessionAsync(string founderId)
    {
        return await _dbSet
            .Where(s => s.FounderId == founderId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Session>> GetCompletedSessionsByFounderAsync(string founderId)
    {
        return await _dbSet
            .Where(s => s.FounderId == founderId && s.Status == SessionStatus.Completed)
            .OrderByDescending(s => s.CompletedAt)
            .ToListAsync();
    }
}
