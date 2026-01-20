using StartupAgent.Shared.Models.Booking;
using StartupAgent.Shared.Services.Email;

namespace StartupAgent.Shared.Services.Booking;

/// <summary>
/// Service for managing booking operations
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Process a booking confirmation and send email
    /// </summary>
    Task<BookingConfirmationResult> ProcessBookingConfirmationAsync(
        BookingConfirmation booking,
        CancellationToken cancellationToken = default);
}

public class BookingService : IBookingService
{
    private readonly IBookingEmailService _emailService;

    public BookingService(IBookingEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<BookingConfirmationResult> ProcessBookingConfirmationAsync(
        BookingConfirmation booking,
        CancellationToken cancellationToken = default)
    {
        var result = new BookingConfirmationResult
        {
            BookingId = booking.BookingId,
            CorrelationId = booking.CorrelationId,
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            // Send confirmation email
            var emailResult = await _emailService.SendBookingConfirmationAsync(booking, cancellationToken);

            result.EmailSendSuccess = emailResult.Success;
            result.EmailError = emailResult.ErrorMessage;
            result.EmailRetryCount = emailResult.RetryCount;

            if (emailResult.Success)
            {
                result.Success = true;
                Console.WriteLine(
                    $"[{result.CorrelationId}] Booking confirmation process completed successfully for {booking.BookingId}");
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to send confirmation email: {emailResult.ErrorMessage}";
                
                // Log but don't fail - booking is still confirmed
                Console.WriteLine(
                    $"[{result.CorrelationId}] Warning: Email send failed for booking {booking.BookingId}, but booking is still valid");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Booking confirmation error: {ex.Message}";
            Console.WriteLine(
                $"[{result.CorrelationId}] Error processing booking confirmation: {ex.Message}");
        }

        return result;
    }
}

public class BookingConfirmationResult
{
    public required string BookingId { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
    
    /// <summary>
    /// Email-specific send result
    /// </summary>
    public bool EmailSendSuccess { get; set; }
    public string? EmailError { get; set; }
    public int EmailRetryCount { get; set; }
}
