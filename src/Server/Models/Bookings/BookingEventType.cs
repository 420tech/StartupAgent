namespace StartupAgent.Models.Bookings;

/// <summary>
/// Event types in the booking funnel
/// </summary>
public enum BookingEventType
{
    /// <summary>
    /// Diagnostic assessment completed
    /// </summary>
    DiagnosticCompleted = 1,

    /// <summary>
    /// CTA button clicked to open booking modal
    /// </summary>
    CtaClicked = 2,

    /// <summary>
    /// Booking confirmed via Calendly webhook
    /// </summary>
    BookingConfirmed = 3,

    /// <summary>
    /// Confirmation email successfully sent
    /// </summary>
    EmailSent = 4,

    /// <summary>
    /// Confirmation email send failed
    /// </summary>
    EmailFailed = 5,

    /// <summary>
    /// Email retry succeeded after initial failure
    /// </summary>
    EmailRetrySucceeded = 6
}
