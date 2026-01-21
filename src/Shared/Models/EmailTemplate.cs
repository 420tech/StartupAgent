namespace StartupAgent.Shared.Models;

/// <summary>
/// Email template type/category for organizing templates by purpose.
/// </summary>
public enum EmailTemplateType
{
    /// <summary>Session start confirmation</summary>
    SessionStart = 1,

    /// <summary>Session completion and results delivery</summary>
    SessionCompletion = 2,

    /// <summary>Session recovery/resumption invitation</summary>
    SessionRecovery = 3,

    /// <summary>Pitch deck analysis results</summary>
    DeckAnalysisResults = 4,

    /// <summary>Booking confirmation</summary>
    BookingConfirmation = 5,

    /// <summary>Booking reminder</summary>
    BookingReminder = 6,

    /// <summary>Transactional email (generic)</summary>
    Transactional = 7,

    /// <summary>Marketing/promotional email</summary>
    Promotional = 8,

    /// <summary>Notification email</summary>
    Notification = 9
}

/// <summary>
/// Supported languages for email templates.
/// </summary>
public enum EmailTemplateLanguage
{
    /// <summary>English</summary>
    English = 1,

    /// <summary>Spanish</summary>
    Spanish = 2,

    /// <summary>French</summary>
    French = 3,

    /// <summary>German</summary>
    German = 4,

    /// <summary>Simplified Chinese</summary>
    ChineseSimplified = 5
}

/// <summary>
/// Email template containing subject, HTML body, and plain text body.
/// Supports versioning and A/B testing.
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Template name/code (e.g., "session-recovery-email", "deck-analysis-results").
    /// Used to identify templates in code.
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template type/category.
    /// </summary>
    public EmailTemplateType Type { get; set; } = EmailTemplateType.Transactional;

    /// <summary>
    /// Template language.
    /// </summary>
    public EmailTemplateLanguage Language { get; set; } = EmailTemplateLanguage.English;

    /// <summary>
    /// Email subject line.
    /// Can include template variables like {{founderName}}.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body.
    /// Supports template variables like {{founderName}}, {{resumeLink}}, etc.
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body (fallback).
    /// Supports same template variables as HTML.
    /// </summary>
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Description of template purpose and usage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Template variables used (comma-separated).
    /// Examples: founderName, sessionId, resumeLink, decisionUrl
    /// Useful for documentation and validation.
    /// </summary>
    public string? Variables { get; set; }

    /// <summary>
    /// Is this the active/published version?
    /// Only one version per template/language combination should be active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number for tracking changes (auto-incremented).
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// A/B test variant (null = control, "A", "B", etc. for variants).
    /// Multiple variants can exist for A/B testing.
    /// </summary>
    public string? ABTestVariant { get; set; }

    /// <summary>
    /// A/B test ID this variant belongs to (if any).
    /// Used to group related variants.
    /// </summary>
    public string? ABTestId { get; set; }

    /// <summary>
    /// Who created this template.
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    /// <summary>
    /// When the template was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who last updated this template.
    /// </summary>
    public string UpdatedBy { get; set; } = "system";

    /// <summary>
    /// When the template was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was published/activated.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Change notes describing what was modified in this version.
    /// </summary>
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// Whether this template is archived (soft delete).
    /// </summary>
    public bool IsArchived { get; set; } = false;
}

/// <summary>
/// Audit history tracking all versions of an email template.
/// </summary>
public class EmailTemplateVersion
{
    /// <summary>
    /// Unique identifier for this version record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID of the template this version belongs to.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Email subject at this version.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML body at this version.
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text body at this version.
    /// </summary>
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Who created this version.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this version was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Change notes describing what was modified.
    /// </summary>
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// When this version was published/activated.
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}

/// <summary>
/// A/B test tracking for template variants.
/// </summary>
public class EmailTemplateABTest
{
    /// <summary>
    /// Unique identifier for this A/B test.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Template code being tested.
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// Test name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Test description and goals.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the A/B test.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End date of the A/B test (null if ongoing).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Is this test currently active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Control variant (baseline to compare against).
    /// </summary>
    public string ControlVariant { get; set; } = "control";

    /// <summary>
    /// Test variants being compared (comma-separated).
    /// </summary>
    public string TestVariants { get; set; } = string.Empty;

    /// <summary>
    /// Winning variant after test completion.
    /// </summary>
    public string? WinnerVariant { get; set; }

    /// <summary>
    /// Number of emails sent for control variant.
    /// </summary>
    public int ControlSentCount { get; set; } = 0;

    /// <summary>
    /// Number of emails sent for test variants (combined).
    /// </summary>
    public int VariantSentCount { get; set; } = 0;

    /// <summary>
    /// Who created this test.
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    /// <summary>
    /// When the test was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the test was concluded.
    /// </summary>
    public DateTime? ConcludedAt { get; set; }
}
