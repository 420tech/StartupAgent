namespace StartupAgent.Models.Bookings;

/// <summary>
/// Event source - where the booking originated from
/// </summary>
public enum BookingEventSource
{
    /// <summary>
    /// Results page CTA button
    /// </summary>
    ResultsPage = 1,

    /// <summary>
    /// Recovery email or other marketing channel
    /// </summary>
    RecoveryEmail = 2,

    /// <summary>
    /// Direct booking link or referral
    /// </summary>
    DirectLink = 3,

    /// <summary>
    /// Source unknown
    /// </summary>
    Unknown = 99
}
