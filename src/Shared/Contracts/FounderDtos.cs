namespace StartupAgent.Shared.Contracts;

/// <summary>
/// Request DTO for creating or updating a founder profile.
/// </summary>
public class CreateUpdateFounderDto
{
    /// <summary>
    /// Founder's display name (optional).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Startup/company name (optional).
    /// </summary>
    public string? StartupName { get; set; }
}

/// <summary>
/// Response DTO for founder profile information.
/// </summary>
public class FounderDto
{
    /// <summary>
    /// Unique identifier for the founder.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Founder's display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Startup/company name.
    /// </summary>
    public string? StartupName { get; set; }

    /// <summary>
    /// Last detected mindset type.
    /// </summary>
    public string? LastMindset { get; set; }

    /// <summary>
    /// Timestamp when profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
