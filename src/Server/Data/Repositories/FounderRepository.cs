using Microsoft.EntityFrameworkCore;
using StartupAgent.Shared.Models;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository implementation for Founder entity.
/// </summary>
public class FounderRepository : Repository<Founder>, IFounderRepository
{
    public FounderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Founder?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(f => f.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(f => f.Email == email);
    }
}
