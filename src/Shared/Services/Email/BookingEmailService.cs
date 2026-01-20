using StartupAgent.Shared.Models.Booking;

namespace StartupAgent.Shared.Services.Email;

/// <summary>
/// Service for sending booking confirmation emails
/// </summary>
public interface IBookingEmailService
{
    /// <summary>
    /// Send booking confirmation email with payment link
    /// </summary>
    Task<EmailSendResult> SendBookingConfirmationAsync(
        BookingConfirmation booking,
        CancellationToken cancellationToken = default);
}

public class BookingEmailService : IBookingEmailService
{
    private const int MaxRetries = 3;
    private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public async Task<EmailSendResult> SendBookingConfirmationAsync(
        BookingConfirmation booking,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        EmailSendResult? lastResult = null;

        while (retryCount < MaxRetries)
        {
            try
            {
                lastResult = await SendEmailAsync(booking, cancellationToken);

                if (lastResult.Success)
                {
                    // Log success
                    Console.WriteLine(
                        $"[{lastResult.CorrelationId}] Booking confirmation email sent successfully to {booking.FounderEmail} (Booking: {booking.BookingId})");
                    return lastResult;
                }

                // If not successful, increment retry count
                retryCount++;

                if (retryCount < MaxRetries)
                {
                    Console.WriteLine(
                        $"[{lastResult.CorrelationId}] Email send failed: {lastResult.ErrorMessage}. Retrying ({retryCount}/{MaxRetries})...");
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine(
                    $"[{booking.CorrelationId}] Exception sending email: {ex.Message}. Retry {retryCount}/{MaxRetries}");

                if (retryCount < MaxRetries)
                {
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }

        // All retries exhausted
        var finalResult = EmailSendResult.CreateFailure(
            $"Failed to send booking confirmation email after {MaxRetries} attempts",
            booking.CorrelationId,
            canRetry: false);

        finalResult.RetryCount = retryCount;

        Console.WriteLine(
            $"[{finalResult.CorrelationId}] Email send permanently failed after {retryCount} retries for booking {booking.BookingId}");

        return finalResult;
    }

    private async Task<EmailSendResult> SendEmailAsync(
        BookingConfirmation booking,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        try
        {
            // TODO: Integrate with SendGrid, Mailgun, or other email service
            // For now, return success with placeholder implementation
            // In production, this would:
            // 1. Call email provider API
            // 2. Handle rate limiting
            // 3. Track delivery status
            // 4. Implement idempotency (using CorrelationId)

            var emailContent = GenerateConfirmationEmailHtml(booking);

            // Placeholder: Simulate email send
            Console.WriteLine(
                $"[{booking.CorrelationId}] Sending confirmation email to {booking.FounderEmail}...");

            // TODO: Uncomment when email provider is configured
            // var result = await emailProvider.SendEmailAsync(
            //     to: booking.FounderEmail,
            //     subject: "Your Strategy Call with Tim is Confirmed",
            //     htmlContent: emailContent,
            //     correlationId: booking.CorrelationId,
            //     cancellationToken: cancellationToken);

            return EmailSendResult.CreateSuccess(booking.CorrelationId);
        }
        catch (Exception ex)
        {
            return EmailSendResult.CreateFailure(
                $"Email service error: {ex.Message}",
                booking.CorrelationId,
                canRetry: true);
        }
    }

    private string GenerateConfirmationEmailHtml(BookingConfirmation booking)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #0EA5E9, #06B6D4); color: white; padding: 24px; border-radius: 8px; margin-bottom: 24px; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 24px; }}
        .detail {{ margin: 12px 0; }}
        .detail-label {{ color: #666; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; }}
        .detail-value {{ font-size: 16px; font-weight: 600; color: #0EA5E9; }}
        .cta-button {{ display: inline-block; background: linear-gradient(135deg, #0EA5E9, #06B6D4); color: white; padding: 14px 32px; border-radius: 6px; text-decoration: none; font-weight: 600; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; text-align: center; margin-top: 32px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Strategy Call Confirmed! 📅</h1>
        </div>
        
        <div class=""content"">
            <p>Hi {booking.FounderName},</p>
            
            <p>Your {booking.DurationMinutes}-minute strategy call with Tim is confirmed. Here are the details:</p>
            
            <div class=""detail"">
                <div class=""detail-label"">Scheduled Date & Time</div>
                <div class=""detail-value"">{booking.ScheduledAt:MMMM d, yyyy 'at' h:mm tt} UTC</div>
            </div>
            
            <div class=""detail"">
                <div class=""detail-label"">Duration</div>
                <div class=""detail-value"">{booking.DurationMinutes} minutes</div>
            </div>
            
            <div class=""detail"">
                <div class=""detail-label"">Price</div>
                <div class=""detail-value"">${booking.PriceUsd}</div>
            </div>
            
            <p>To complete payment and secure your spot, click the button below:</p>
            
            <a href=""{booking.PaymentLink}"" class=""cta-button"">Complete Payment</a>
            
            <p>A Zoom link will be sent to your email once payment is confirmed.</p>
            
            <p>Questions? Reply to this email and we'll help you out.</p>
            
            <p>Best,<br>Tim from StartupAgent</p>
        </div>
        
        <div class=""footer"">
            <p>Booking ID: {booking.BookingId}<br>
            Session ID: {booking.SessionId}</p>
        </div>
    </div>
</body>
</html>";
    }
}
