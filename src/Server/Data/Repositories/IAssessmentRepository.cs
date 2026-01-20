using StartupAgent.Shared.Models;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository interface for Assessment data access.
/// </summary>
public interface IAssessmentRepository : IRepository<Assessment>
{
    /// <summary>
    /// Get all assessments for a specific founder, ordered by most recent first.
    /// </summary>
    Task<IEnumerable<Assessment>> GetAssessmentsByFounderAsync(string founderId);

    /// <summary>
    /// Get the most recent completed assessment for a founder.
    /// </summary>
    Task<Assessment?> GetMostRecentAssessmentAsync(string founderId);

    /// <summary>
    /// Get assessments by completion status.
    /// </summary>
    Task<IEnumerable<Assessment>> GetAssessmentsByStatusAsync(ReportStatus status);
}
