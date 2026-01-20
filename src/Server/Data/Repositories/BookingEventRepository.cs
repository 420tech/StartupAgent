using StartupAgent.Models.Bookings;

namespace StartupAgent.Data.Repositories;

/// <summary>
/// Repository interface for booking event tracking
/// </summary>
public interface IBookingEventRepository
{
    /// <summary>
    /// Record a new booking event
    /// </summary>
    Task<BookingEvent> RecordEventAsync(
        string founderId,
        BookingEventType eventType,
        BookingEventSource source,
        string? correlationId = null,
        Guid? sessionId = null,
        string? bookingId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all events for a founder
    /// </summary>
    Task<IEnumerable<BookingEvent>> GetFounderEventsAsync(
        string founderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events by correlation ID (for tracing a booking workflow)
    /// </summary>
    Task<IEnumerable<BookingEvent>> GetEventsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events for a founder within a date range
    /// </summary>
    Task<IEnumerable<BookingEvent>> GetFounderEventsByDateRangeAsync(
        string founderId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events by type and source
    /// </summary>
    Task<IEnumerable<BookingEvent>> GetEventsByTypeAndSourceAsync(
        BookingEventType eventType,
        BookingEventSource source,
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get booking confirmed events with details
    /// </summary>
    Task<IEnumerable<BookingEvent>> GetBookingConfirmedEventsAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count CTA clicks for funnel analysis
    /// </summary>
    Task<int> CountCtaClicksAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count bookings confirmed for funnel analysis
    /// </summary>
    Task<int> CountBookingsConfirmedAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversion rate (bookings confirmed / CTA clicks)
    /// </summary>
    Task<double> GetConversionRateAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository implementation for booking events
/// </summary>
public class BookingEventRepository(ApplicationDbContext context) : IBookingEventRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<BookingEvent> RecordEventAsync(
        string founderId,
        BookingEventType eventType,
        BookingEventSource source,
        string? correlationId = null,
        Guid? sessionId = null,
        string? bookingId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var @event = new BookingEvent
        {
            Id = Guid.NewGuid(),
            FounderId = founderId,
            EventType = eventType,
            Source = source,
            CorrelationId = correlationId,
            SessionId = sessionId,
            BookingId = bookingId,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };

        _context.BookingEvents.Add(@event);
        await _context.SaveChangesAsync(cancellationToken);

        return @event;
    }

    public async Task<IEnumerable<BookingEvent>> GetFounderEventsAsync(
        string founderId,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(
            _context.BookingEvents
                .Where(e => e.FounderId == founderId)
                .OrderByDescending(e => e.CreatedAt)
                .AsEnumerable());
    }

    public async Task<IEnumerable<BookingEvent>> GetEventsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(
            _context.BookingEvents
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.CreatedAt)
                .AsEnumerable());
    }

    public async Task<IEnumerable<BookingEvent>> GetFounderEventsByDateRangeAsync(
        string founderId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(
            _context.BookingEvents
                .Where(e => e.FounderId == founderId
                    && e.CreatedAt >= startDate
                    && e.CreatedAt <= endDate)
                .OrderByDescending(e => e.CreatedAt)
                .AsEnumerable());
    }

    public async Task<IEnumerable<BookingEvent>> GetEventsByTypeAndSourceAsync(
        BookingEventType eventType,
        BookingEventSource source,
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BookingEvents
            .Where(e => e.EventType == eventType && e.Source == source);

        if (sinceDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= sinceDate.Value);
        }

        return await Task.FromResult(
            query.OrderByDescending(e => e.CreatedAt).AsEnumerable());
    }

    public async Task<IEnumerable<BookingEvent>> GetBookingConfirmedEventsAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BookingEvents
            .Where(e => e.EventType == BookingEventType.BookingConfirmed);

        if (sinceDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= sinceDate.Value);
        }

        return await Task.FromResult(
            query.OrderByDescending(e => e.CreatedAt).AsEnumerable());
    }

    public async Task<int> CountCtaClicksAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BookingEvents
            .Where(e => e.EventType == BookingEventType.CtaClicked);

        if (sinceDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= sinceDate.Value);
        }

        return await Task.FromResult(query.Count());
    }

    public async Task<int> CountBookingsConfirmedAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BookingEvents
            .Where(e => e.EventType == BookingEventType.BookingConfirmed);

        if (sinceDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= sinceDate.Value);
        }

        return await Task.FromResult(query.Count());
    }

    public async Task<double> GetConversionRateAsync(
        DateTime? sinceDate = null,
        CancellationToken cancellationToken = default)
    {
        var ctaClicks = await CountCtaClicksAsync(sinceDate, cancellationToken);
        if (ctaClicks == 0)
        {
            return 0.0;
        }

        var bookingsConfirmed = await CountBookingsConfirmedAsync(sinceDate, cancellationToken);
        return (double)bookingsConfirmed / ctaClicks;
    }
}
