using StartupAgent.Models.Bookings;
using StartupAgent.Shared.Models.Booking;
using StartupAgent.Shared.Services.Booking;
using StartupAgent.Data.Repositories;

namespace StartupAgent.Server.Services.Bookings;

/// <summary>
/// Booking service with event tracking integration
/// </summary>
public interface IBookingEventTrackingService
{
    /// <summary>
    /// Process booking confirmation with event tracking
    /// </summary>
    Task<BookingConfirmationResult> ProcessBookingWithTrackingAsync(
        BookingConfirmation booking,
        string founderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a funnel event
    /// </summary>
    Task RecordFunnelEventAsync(
        string founderId,
        BookingEventType eventType,
        BookingEventSource source,
        string? correlationId = null,
        Guid? sessionId = null,
        string? bookingId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of booking service with event tracking
/// </summary>
public class BookingEventTrackingService(
    IBookingService bookingService,
    IBookingEventRepository eventRepository) : IBookingEventTrackingService
{
    private readonly IBookingService _bookingService = bookingService;
    private readonly IBookingEventRepository _eventRepository = eventRepository;

    public async Task<BookingConfirmationResult> ProcessBookingWithTrackingAsync(
        BookingConfirmation booking,
        string founderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Record booking confirmed event
            await _eventRepository.RecordEventAsync(
                founderId,
                BookingEventType.BookingConfirmed,
                BookingEventSource.ResultsPage,
                booking.CorrelationId,
                null, // SessionId - can be extracted from booking if available
                booking.BookingId,
                null,
                cancellationToken);

            Console.WriteLine(
                $"[{booking.CorrelationId}] BookingConfirmed event recorded for {founderId}");

            // Process booking (sends email, etc.)
            var result = await _bookingService.ProcessBookingConfirmationAsync(booking, cancellationToken);

            // Record email send event outcome
            if (result.EmailSendSuccess)
            {
                await _eventRepository.RecordEventAsync(
                    founderId,
                    BookingEventType.EmailSent,
                    BookingEventSource.ResultsPage,
                    booking.CorrelationId,
                    null,
                    booking.BookingId,
                    $"{{\"retryCount\":{result.EmailRetryCount}}}",
                    cancellationToken);

                Console.WriteLine(
                    $"[{booking.CorrelationId}] EmailSent event recorded for {founderId}");
            }
            else
            {
                await _eventRepository.RecordEventAsync(
                    founderId,
                    BookingEventType.EmailFailed,
                    BookingEventSource.ResultsPage,
                    booking.CorrelationId,
                    null,
                    booking.BookingId,
                    $"{{\"error\":\"{result.EmailError}\",\"retryCount\":{result.EmailRetryCount}}}",
                    cancellationToken);

                Console.WriteLine(
                    $"[{booking.CorrelationId}] EmailFailed event recorded for {founderId}: {result.EmailError}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[{booking.CorrelationId}] Error in booking event tracking: {ex.Message}");
            
            // Still process the booking even if tracking fails
            return await _bookingService.ProcessBookingConfirmationAsync(booking, cancellationToken);
        }
    }

    public async Task RecordFunnelEventAsync(
        string founderId,
        BookingEventType eventType,
        BookingEventSource source,
        string? correlationId = null,
        Guid? sessionId = null,
        string? bookingId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _eventRepository.RecordEventAsync(
                founderId,
                eventType,
                source,
                correlationId,
                sessionId,
                bookingId,
                null,
                cancellationToken);

            Console.WriteLine(
                $"[{correlationId}] {eventType} event recorded for {founderId} from {source}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[{correlationId}] Error recording funnel event: {ex.Message}");
            
            // Don't throw - event recording failure shouldn't block application flow
        }
    }
}
