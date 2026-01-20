namespace StartupAgent.Shared.Models;

/// <summary>
/// Founder entity representing a user of the StartupAgent platform.
/// </summary>
public class Founder
{
    /// <summary>
    /// Unique identifier for the founder (primary key).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Email address (used for login and communication).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Founder's display name (optional).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Startup name (optional).
    /// </summary>
    public string? StartupName { get; set; }

    /// <summary>
    /// Founder's mindset type (from last assessment).
    /// </summary>
    public MindsetType? LastMindset { get; set; }

    /// <summary>
    /// Timestamp when founder account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when founder account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to founder's sessions.
    /// </summary>
    public ICollection<Session> Sessions { get; set; } = new List<Session>();

    /// <summary>
    /// Navigation property to founder's assessments.
    /// </summary>
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
