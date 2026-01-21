using StartupAgent.Data;
using StartupAgent.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository interface for email template persistence.
/// </summary>
public interface IEmailTemplateRepository
{
    /// <summary>
    /// Get active template by code and language.
    /// </summary>
    Task<EmailTemplate?> GetActiveTemplateAsync(string templateCode, EmailTemplateLanguage language);

    /// <summary>
    /// Get all templates for a template code (all languages and versions).
    /// </summary>
    Task<List<EmailTemplate>> GetTemplatesByCodeAsync(string templateCode);

    /// <summary>
    /// Get all active templates by type.
    /// </summary>
    Task<List<EmailTemplate>> GetActiveTemplatesByTypeAsync(EmailTemplateType type);

    /// <summary>
    /// Create a new template.
    /// </summary>
    Task AddAsync(EmailTemplate template);

    /// <summary>
    /// Update an existing template.
    /// </summary>
    Task UpdateAsync(EmailTemplate template);

    /// <summary>
    /// Get template by ID.
    /// </summary>
    Task<EmailTemplate?> GetByIdAsync(string id);

    /// <summary>
    /// Save changes to database.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Get version history for a template.
    /// </summary>
    Task<List<EmailTemplateVersion>> GetVersionHistoryAsync(string templateId);

    /// <summary>
    /// Add version record for audit trail.
    /// </summary>
    Task AddVersionAsync(EmailTemplateVersion version);

    /// <summary>
    /// Get active A/B tests for a template code.
    /// </summary>
    Task<EmailTemplateABTest?> GetActiveABTestAsync(string templateCode);

    /// <summary>
    /// Add A/B test.
    /// </summary>
    Task AddABTestAsync(EmailTemplateABTest test);

    /// <summary>
    /// Update A/B test.
    /// </summary>
    Task UpdateABTestAsync(EmailTemplateABTest test);
}

/// <summary>
/// Implementation of email template repository.
/// </summary>
public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateRepository> _logger;

    public EmailTemplateRepository(
        ApplicationDbContext context,
        ILogger<EmailTemplateRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailTemplate?> GetActiveTemplateAsync(string templateCode, EmailTemplateLanguage language)
    {
        return await _context.EmailTemplates
            .Where(t => t.TemplateCode == templateCode
                && t.Language == language
                && t.IsActive
                && !t.IsArchived)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<List<EmailTemplate>> GetTemplatesByCodeAsync(string templateCode)
    {
        return await _context.EmailTemplates
            .Where(t => t.TemplateCode == templateCode && !t.IsArchived)
            .OrderByDescending(t => t.Version)
            .ToListAsync();
    }

    public async Task<List<EmailTemplate>> GetActiveTemplatesByTypeAsync(EmailTemplateType type)
    {
        return await _context.EmailTemplates
            .Where(t => t.Type == type && t.IsActive && !t.IsArchived)
            .ToListAsync();
    }

    public async Task AddAsync(EmailTemplate template)
    {
        _context.EmailTemplates.Add(template);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(EmailTemplate template)
    {
        _context.EmailTemplates.Update(template);
        await SaveChangesAsync();
    }

    public async Task<EmailTemplate?> GetByIdAsync(string id)
    {
        return await _context.EmailTemplates.FindAsync(id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<EmailTemplateVersion>> GetVersionHistoryAsync(string templateId)
    {
        return await _context.EmailTemplateVersions
            .Where(v => v.TemplateId == templateId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();
    }

    public async Task AddVersionAsync(EmailTemplateVersion version)
    {
        _context.EmailTemplateVersions.Add(version);
        await SaveChangesAsync();
    }

    public async Task<EmailTemplateABTest?> GetActiveABTestAsync(string templateCode)
    {
        return await _context.EmailTemplateABTests
            .Where(t => t.TemplateCode == templateCode && t.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task AddABTestAsync(EmailTemplateABTest test)
    {
        _context.EmailTemplateABTests.Add(test);
        await SaveChangesAsync();
    }

    public async Task UpdateABTestAsync(EmailTemplateABTest test)
    {
        _context.EmailTemplateABTests.Update(test);
        await SaveChangesAsync();
    }
}
