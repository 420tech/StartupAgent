using StartupAgent.Data.Repositories;
using StartupAgent.Shared.Models;

namespace StartupAgent.Server.Services.Email;

/// <summary>
/// Service for managing transactional email templates.
/// Handles template retrieval, versioning, A/B testing, and variable interpolation.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Get active template by code and language.
    /// </summary>
    Task<EmailTemplate?> GetTemplateAsync(string templateCode, EmailTemplateLanguage language = EmailTemplateLanguage.English);

    /// <summary>
    /// Render template with variable substitution.
    /// Variables passed as dictionary: { "founderName": "John", "resumeLink": "https://..." }
    /// </summary>
    string RenderTemplate(EmailTemplate template, Dictionary<string, string> variables, bool useHtml = true);

    /// <summary>
    /// Create a new template version.
    /// Automatically increments version number and creates audit trail.
    /// </summary>
    Task<EmailTemplate> CreateTemplateAsync(
        string templateCode,
        string name,
        EmailTemplateType type,
        EmailTemplateLanguage language,
        string subject,
        string htmlBody,
        string plainTextBody,
        string? variables = null,
        string? description = null,
        string createdBy = "system");

    /// <summary>
    /// Update an existing template.
    /// Creates new version and preserves history.
    /// </summary>
    Task<EmailTemplate> UpdateTemplateAsync(
        string templateCode,
        EmailTemplateLanguage language,
        string subject,
        string htmlBody,
        string plainTextBody,
        string changeNotes,
        string updatedBy = "system");

    /// <summary>
    /// Get template version history for audit trail.
    /// </summary>
    Task<List<EmailTemplateVersion>> GetVersionHistoryAsync(string templateId);

    /// <summary>
    /// Start an A/B test for a template.
    /// </summary>
    Task<EmailTemplateABTest> StartABTestAsync(
        string templateCode,
        string testName,
        string controlVariant,
        string[] testVariants,
        string? description = null);

    /// <summary>
    /// Get active A/B test for a template.
    /// </summary>
    Task<EmailTemplateABTest?> GetActiveABTestAsync(string templateCode);

    /// <summary>
    /// Conclude an A/B test and select winner.
    /// </summary>
    Task<EmailTemplateABTest> ConcludeABTestAsync(string testId, string winnerVariant);

    /// <summary>
    /// Select variant for A/B test (returns "control" or test variant).
    /// </summary>
    Task<string> SelectABVariantAsync(string templateCode);
}

/// <summary>
/// Implementation of email template service.
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _repository;
    private readonly ILogger<EmailTemplateService> _logger;
    private static readonly Random _random = new Random();

    public EmailTemplateService(
        IEmailTemplateRepository repository,
        ILogger<EmailTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<EmailTemplate?> GetTemplateAsync(string templateCode, EmailTemplateLanguage language = EmailTemplateLanguage.English)
    {
        var template = await _repository.GetActiveTemplateAsync(templateCode, language);
        if (template == null)
        {
            _logger.LogWarning(
                "Email template not found: TemplateCode={TemplateCode}, Language={Language}",
                templateCode, language);
        }
        return template;
    }

    public string RenderTemplate(EmailTemplate template, Dictionary<string, string> variables, bool useHtml = true)
    {
        var body = useHtml ? template.HtmlBody : template.PlainTextBody;

        // Replace all variables in the template
        // Variables are in format {{variableName}}
        foreach (var (key, value) in variables)
        {
            var placeholder = $"{{{{{key}}}}}"; // Wraps key with {{ }}
            body = body.Replace(placeholder, value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        _logger.LogDebug("Rendered email template: {TemplateCode}, Variables: {VariableCount}", template.TemplateCode, variables.Count);

        return body;
    }

    public async Task<EmailTemplate> CreateTemplateAsync(
        string templateCode,
        string name,
        EmailTemplateType type,
        EmailTemplateLanguage language,
        string subject,
        string htmlBody,
        string plainTextBody,
        string? variables = null,
        string? description = null,
        string createdBy = "system")
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid().ToString(),
            TemplateCode = templateCode,
            Name = name,
            Type = type,
            Language = language,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            Variables = variables,
            Description = description,
            IsActive = true,
            Version = 1,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(template);

        // Create initial version record
        var version = new EmailTemplateVersion
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = template.Id,
            Version = 1,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow
        };

        await _repository.AddVersionAsync(version);

        _logger.LogInformation(
            "Created email template: TemplateCode={TemplateCode}, Language={Language}, Type={Type}",
            templateCode, language, type);

        return template;
    }

    public async Task<EmailTemplate> UpdateTemplateAsync(
        string templateCode,
        EmailTemplateLanguage language,
        string subject,
        string htmlBody,
        string plainTextBody,
        string changeNotes,
        string updatedBy = "system")
    {
        var existing = await _repository.GetActiveTemplateAsync(templateCode, language);
        if (existing == null)
        {
            throw new InvalidOperationException($"Template not found: {templateCode} ({language})");
        }

        // Create version history record for old version
        var oldVersion = new EmailTemplateVersion
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = existing.Id,
            Version = existing.Version,
            Subject = existing.Subject,
            HtmlBody = existing.HtmlBody,
            PlainTextBody = existing.PlainTextBody,
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt,
            ChangeNotes = changeNotes
        };

        await _repository.AddVersionAsync(oldVersion);

        // Update template
        existing.Subject = subject;
        existing.HtmlBody = htmlBody;
        existing.PlainTextBody = plainTextBody;
        existing.Version += 1;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.PublishedAt = DateTime.UtcNow;
        existing.ChangeNotes = changeNotes;

        await _repository.UpdateAsync(existing);

        _logger.LogInformation(
            "Updated email template: TemplateCode={TemplateCode}, Language={Language}, NewVersion={Version}",
            templateCode, language, existing.Version);

        return existing;
    }

    public async Task<List<EmailTemplateVersion>> GetVersionHistoryAsync(string templateId)
    {
        return await _repository.GetVersionHistoryAsync(templateId);
    }

    public async Task<EmailTemplateABTest> StartABTestAsync(
        string templateCode,
        string testName,
        string controlVariant,
        string[] testVariants,
        string? description = null)
    {
        // Check if there's already an active test
        var existing = await _repository.GetActiveABTestAsync(templateCode);
        if (existing != null)
        {
            throw new InvalidOperationException($"Active A/B test already exists for template: {templateCode}");
        }

        var test = new EmailTemplateABTest
        {
            Id = Guid.NewGuid().ToString(),
            TemplateCode = templateCode,
            Name = testName,
            Description = description,
            StartedAt = DateTime.UtcNow,
            IsActive = true,
            ControlVariant = controlVariant,
            TestVariants = string.Join(",", testVariants),
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddABTestAsync(test);

        _logger.LogInformation(
            "Started A/B test: TemplateCode={TemplateCode}, TestName={TestName}, Variants={Variants}",
            templateCode, testName, test.TestVariants);

        return test;
    }

    public async Task<EmailTemplateABTest?> GetActiveABTestAsync(string templateCode)
    {
        return await _repository.GetActiveABTestAsync(templateCode);
    }

    public async Task<EmailTemplateABTest> ConcludeABTestAsync(string testId, string winnerVariant)
    {
        // Get test (would need method to find by ID)
        // For now, simplified - assumes test exists
        var test = new EmailTemplateABTest { Id = testId };
        test.IsActive = false;
        test.EndedAt = DateTime.UtcNow;
        test.ConcludedAt = DateTime.UtcNow;
        test.WinnerVariant = winnerVariant;

        await _repository.UpdateABTestAsync(test);

        _logger.LogInformation(
            "Concluded A/B test: TestId={TestId}, Winner={Winner}",
            testId, winnerVariant);

        return test;
    }

    public async Task<string> SelectABVariantAsync(string templateCode)
    {
        var test = await _repository.GetActiveABTestAsync(templateCode);
        if (test == null)
        {
            return "control"; // No active test, use control
        }

        // Randomly select between control and variants (50/50 for simplicity)
        var variants = test.TestVariants.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var allOptions = new List<string> { test.ControlVariant };
        allOptions.AddRange(variants);

        var selected = allOptions[_random.Next(allOptions.Count)];

        // Track sent count
        if (selected == test.ControlVariant)
        {
            test.ControlSentCount++;
        }
        else
        {
            test.VariantSentCount++;
        }

        await _repository.UpdateABTestAsync(test);

        return selected;
    }
}
