using StartupAgent.Shared.Models;

namespace StartupAgent.Shared.DTOs.Email;

/// <summary>
/// DTO for creating a new email template.
/// </summary>
public class CreateEmailTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EmailTemplateType Type { get; set; }
    public EmailTemplateLanguage Language { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public string? Variables { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an email template.
/// </summary>
public class UpdateEmailTemplateDto
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public string ChangeNotes { get; set; } = string.Empty;
}

/// <summary>
/// DTO for rendering/sending a template with variables.
/// </summary>
public class RenderEmailTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public EmailTemplateLanguage Language { get; set; } = EmailTemplateLanguage.English;
    public Dictionary<string, string> Variables { get; set; } = new();
    public bool UseHtml { get; set; } = true;
}

/// <summary>
/// Response DTO for email template.
/// </summary>
public class EmailTemplateResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EmailTemplateType Type { get; set; }
    public EmailTemplateLanguage Language { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public string? Variables { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public string? ABTestVariant { get; set; }
    public string? ABTestId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ChangeNotes { get; set; }
    public bool IsArchived { get; set; }

    public static EmailTemplateResponseDto FromEmailTemplate(EmailTemplate template)
    {
        return new EmailTemplateResponseDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            Name = template.Name,
            Type = template.Type,
            Language = template.Language,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody,
            PlainTextBody = template.PlainTextBody,
            Variables = template.Variables,
            Description = template.Description,
            IsActive = template.IsActive,
            Version = template.Version,
            ABTestVariant = template.ABTestVariant,
            ABTestId = template.ABTestId,
            CreatedBy = template.CreatedBy,
            CreatedAt = template.CreatedAt,
            UpdatedBy = template.UpdatedBy,
            UpdatedAt = template.UpdatedAt,
            PublishedAt = template.PublishedAt,
            ChangeNotes = template.ChangeNotes,
            IsArchived = template.IsArchived
        };
    }
}

/// <summary>
/// Response DTO for email template version.
/// </summary>
public class EmailTemplateVersionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ChangeNotes { get; set; }

    public static EmailTemplateVersionResponseDto FromTemplateVersion(EmailTemplateVersion version)
    {
        return new EmailTemplateVersionResponseDto
        {
            Id = version.Id,
            TemplateId = version.TemplateId,
            Version = version.Version,
            Subject = version.Subject,
            HtmlBody = version.HtmlBody,
            PlainTextBody = version.PlainTextBody,
            CreatedBy = version.CreatedBy,
            CreatedAt = version.CreatedAt,
            PublishedAt = version.PublishedAt,
            ChangeNotes = version.ChangeNotes
        };
    }
}

/// <summary>
/// DTO for creating an A/B test.
/// </summary>
public class CreateABTestDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string ControlVariant { get; set; } = string.Empty;
    public string[] TestVariants { get; set; } = Array.Empty<string>();
    public string? Description { get; set; }
}

/// <summary>
/// Response DTO for A/B test.
/// </summary>
public class ABTestResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? ConcludedAt { get; set; }
    public bool IsActive { get; set; }
    public string ControlVariant { get; set; } = string.Empty;
    public string TestVariants { get; set; } = string.Empty;
    public int ControlSentCount { get; set; }
    public int VariantSentCount { get; set; }
    public string? WinnerVariant { get; set; }

    public static ABTestResponseDto FromABTest(EmailTemplateABTest test)
    {
        return new ABTestResponseDto
        {
            Id = test.Id,
            TemplateCode = test.TemplateCode,
            Name = test.Name,
            Description = test.Description,
            StartedAt = test.StartedAt,
            EndedAt = test.EndedAt,
            ConcludedAt = test.ConcludedAt,
            IsActive = test.IsActive,
            ControlVariant = test.ControlVariant,
            TestVariants = test.TestVariants,
            ControlSentCount = test.ControlSentCount,
            VariantSentCount = test.VariantSentCount,
            WinnerVariant = test.WinnerVariant
        };
    }
}
