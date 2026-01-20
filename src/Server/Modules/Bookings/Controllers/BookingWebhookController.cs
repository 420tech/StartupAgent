using Microsoft.AspNetCore.Mvc;
using StartupAgent.Shared.Models.Booking;
using StartupAgent.Shared.Services.Booking;

namespace StartupAgent.Server.Modules.Bookings.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class BookingWebhookController(
    IBookingService bookingService) : ControllerBase
{
    private readonly IBookingService _bookingService = bookingService;

    /// <summary>
    /// Webhook endpoint for Calendly booking confirmed events
    /// </summary>
    [HttpPost("calendly/booking-confirmed")]
    public async Task<IActionResult> HandleCalendlyBookingConfirmed(
        [FromBody] CalendlyBookingWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            if (string.IsNullOrEmpty(payload?.Email) || string.IsNullOrEmpty(payload?.EventStartTime))
            {
                return BadRequest(new { error = "Missing email or event time in webhook payload", correlationId });
            }

            // TODO: Fetch session info and founder details from database
            // TODO: Validate webhook signature (Calendly sends X-Calendly-Signature header)
            // TODO: Prevent duplicate processing (check if booking already processed)

            // Parse scheduled time from Calendly event
            var scheduledAt = DateTime.Parse(payload.EventStartTime);

            // Create booking confirmation
            var booking = new BookingConfirmation
            {
                BookingId = payload.EventId ?? Guid.NewGuid().ToString(),
                FounderId = "placeholder-founder-id", // TODO: Map from Calendly
                FounderEmail = payload.Email,
                FounderName = payload.Name ?? "Founder",
                SessionId = "placeholder-session-id", // TODO: Extract from Calendly booking data
                ScheduledAt = scheduledAt,
                DurationMinutes = 30,
                PriceUsd = 97m,
                PaymentLink = "https://checkout.stripe.com/pay/placeholder", // TODO: Generate Stripe link or use config
                CorrelationId = correlationId
            };

            // Process booking (send email, etc.)
            var result = await _bookingService.ProcessBookingConfirmationAsync(booking, cancellationToken);

            if (result.Success)
            {
                return Ok(new
                {
                    message = "Booking confirmation processed",
                    bookingId = result.BookingId,
                    correlationId = result.CorrelationId
                });
            }
            else
            {
                // Log but return 202 Accepted - Calendly shouldn't retry on email failures
                // The booking is valid, just email delivery had issues
                Console.WriteLine(
                    $"[{correlationId}] Booking {booking.BookingId} confirmed but email failed: {result.EmailError}");

                return Accepted(new
                {
                    message = "Booking confirmed (email delivery pending)",
                    bookingId = result.BookingId,
                    warning = result.ErrorMessage,
                    correlationId = result.CorrelationId
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{correlationId}] Error processing Calendly webhook: {ex.Message}");
            
            return StatusCode(500, new
            {
                error = "Failed to process booking confirmation",
                correlationId,
                canRetry = true
            });
        }
    }
}

/// <summary>
/// Calendly webhook payload (simplified - contains booking details)
/// </summary>
public class CalendlyBookingWebhookPayload
{
    public string? EventId { get; set; }
    public string? EventStartTime { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    
    // Additional fields from Calendly can be added as needed
}
