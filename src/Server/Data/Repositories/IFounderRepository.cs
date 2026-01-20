using StartupAgent.Shared.Models;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository interface for Founder entity with specialized queries.
/// </summary>
public interface IFounderRepository : IRepository<Founder>
{
    /// <summary>
    /// Find a founder by email address.
    /// </summary>
    Task<Founder?> GetByEmailAsync(string email);

    /// <summary>
    /// Check if a founder with the given email exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);
}
