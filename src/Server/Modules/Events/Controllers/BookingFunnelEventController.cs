using Microsoft.AspNetCore.Mvc;
using StartupAgent.Models.Bookings;
using StartupAgent.Server.Services.Bookings;

namespace StartupAgent.Server.Modules.Events.Controllers;

[ApiController]
[Route("api/v1/events")]
public class BookingFunnelEventController(
    IBookingEventTrackingService eventTrackingService) : ControllerBase
{
    private readonly IBookingEventTrackingService _eventTrackingService = eventTrackingService;

    /// <summary>
    /// Record a booking funnel event (CTA click, modal view, etc.)
    /// </summary>
    [HttpPost("booking-funnel")]
    public async Task<IActionResult> RecordFunnelEvent(
        [FromBody] BookingFunnelEventPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload == null)
        {
            return BadRequest(new { error = "Missing event payload" });
        }

        try
        {
            // Parse event type
            if (!Enum.TryParse<BookingEventType>(payload.EventType, ignoreCase: true, out var eventType))
            {
                return BadRequest(new { error = $"Invalid event type: {payload.EventType}" });
            }

            // Parse event source
            if (!Enum.TryParse<BookingEventSource>(payload.Source, ignoreCase: true, out var source))
            {
                // Default to Unknown if source not recognized
                source = BookingEventSource.Unknown;
            }

            // Record the event
            await _eventTrackingService.RecordFunnelEventAsync(
                payload.FounderId,
                eventType,
                source,
                payload.CorrelationId,
                string.IsNullOrEmpty(payload.SessionId) ? null : Guid.Parse(payload.SessionId),
                payload.BookingId,
                cancellationToken);

            return Ok(new
            {
                message = "Event recorded",
                correlationId = payload.CorrelationId,
                eventType = payload.EventType,
                recordedAt = DateTime.UtcNow
            });
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid session ID format" });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error recording funnel event: {ex.Message}");
            
            return StatusCode(500, new
            {
                error = "Failed to record event",
                correlationId = payload.CorrelationId
            });
        }
    }
}

/// <summary>
/// Payload for booking funnel events from client
/// </summary>
public class BookingFunnelEventPayload
{
    public required string FounderId { get; set; }
    public required string EventType { get; set; }
    public required string Source { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? BookingId { get; set; }
    public DateTime? Timestamp { get; set; }
}
