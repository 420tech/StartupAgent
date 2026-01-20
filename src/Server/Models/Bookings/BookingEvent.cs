using StartupAgent.Shared.Models;

namespace StartupAgent.Models.Bookings;

/// <summary>
/// Records booking funnel events for conversion tracking and analysis
/// </summary>
public class BookingEvent
{
    public Guid Id { get; set; }

    /// <summary>
    /// Founder who triggered the event (foreign key to Founder.Id which is string)
    /// </summary>
    public string FounderId { get; set; } = string.Empty;

    /// <summary>
    /// Type of event (CTA click, booking confirmed, etc.)
    /// </summary>
    public BookingEventType EventType { get; set; }

    /// <summary>
    /// Where the booking originated from
    /// </summary>
    public BookingEventSource Source { get; set; }

    /// <summary>
    /// Correlation ID for tracing through the entire booking workflow
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Optional session ID (if available at time of event)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Optional booking ID (populated after BookingConfirmed event)
    /// </summary>
    public string? BookingId { get; set; }

    /// <summary>
    /// Additional metadata (JSON or free-form)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
