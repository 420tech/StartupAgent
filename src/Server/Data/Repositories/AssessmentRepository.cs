using StartupAgent.Shared.Models;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Implementation of Assessment repository.
/// </summary>
public class AssessmentRepository : Repository<Assessment>, IAssessmentRepository
{
    public AssessmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Assessment>> GetAssessmentsByFounderAsync(string founderId)
    {
        return await Task.FromResult(
            _dbSet.Where(a => a.FounderId == founderId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList());
    }

    public async Task<Assessment?> GetMostRecentAssessmentAsync(string founderId)
    {
        return await Task.FromResult(
            _dbSet.Where(a => a.FounderId == founderId && a.Status == ReportStatus.Succeeded)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault());
    }

    public async Task<IEnumerable<Assessment>> GetAssessmentsByStatusAsync(ReportStatus status)
    {
        return await Task.FromResult(
            _dbSet.Where(a => a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToList());
    }
}
