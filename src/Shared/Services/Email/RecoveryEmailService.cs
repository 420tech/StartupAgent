using StartupAgent.Shared.Models;
using StartupAgent.Shared.Models.Booking;

namespace StartupAgent.Shared.Services.Email;

/// <summary>
/// Service for sending recovery emails to founders after session drop-off
/// </summary>
public interface IRecoveryEmailService
{
    /// <summary>
    /// Send recovery email with resume link
    /// </summary>
    /// <param name="recoveryEmail">Recovery email record with resume link</param>
    /// <param name="founder">Founder details for personalization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success/failure details</returns>
    Task<EmailSendResult> SendRecoveryEmailAsync(
        RecoveryEmail recoveryEmail,
        Founder founder,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of recovery email service
/// </summary>
public class RecoveryEmailService : IRecoveryEmailService
{
    public async Task<EmailSendResult> SendRecoveryEmailAsync(
        RecoveryEmail recoveryEmail,
        Founder founder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine(
                $"Sending recovery email to founder {founder.Id} at {recoveryEmail.Email}");

            // Build HTML content
            var htmlContent = BuildRecoveryEmailHtml(founder, recoveryEmail);

            // TODO: Replace with actual email service (SendGrid, AWS SES, etc.)
            // For now, simulate email send with delay
            await Task.Delay(1000, cancellationToken);

            Console.WriteLine(
                $"Recovery email sent successfully to {recoveryEmail.Email}");

            return new EmailSendResult
            {
                Success = true,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                $"Recovery email send cancelled for {recoveryEmail.Email}");

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Email send cancelled",
                CanRetry = true
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error sending recovery email to {recoveryEmail.Email}: {ex.Message}");

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = $"Failed to send email: {ex.Message}",
                CanRetry = true
            };
        }
    }

    /// <summary>
    /// Build HTML content for recovery email
    /// </summary>
    private string BuildRecoveryEmailHtml(Founder founder, RecoveryEmail recoveryEmail)
    {
        var displayName = !string.IsNullOrEmpty(founder.DisplayName)
            ? founder.DisplayName
            : founder.Email.Split('@')[0];

        var dropOffTime = recoveryEmail.CreatedAt.ToString("MMM d, yyyy 'at' h:mm tt");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }}
        .secondary-cta {{ margin-top: 30px; padding-top: 30px; border-top: 1px solid #ddd; }}
        .footer {{ text-align: center; font-size: 12px; color: #666; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
        .highlight {{ background: #fffbcd; padding: 15px; border-left: 4px solid #ffc107; border-radius: 4px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome Back, {displayName}!</h1>
        </div>
        <div class=""content"">
            <p>Hi {displayName},</p>
            
            <p>We noticed you stepped away from your StartupAgent diagnostic assessment at <strong>{dropOffTime}</strong>.</p>
            
            <p>No worries! Your progress has been saved, and you can pick up right where you left off.</p>
            
            <div class=""highlight"">
                <strong>Ready to continue?</strong> Click below to resume your assessment and unlock personalized insights for your startup.
            </div>
            
            <a href=""{recoveryEmail.ResumeLink}"" class=""button"">Resume My Assessment</a>
            
            <div class=""secondary-cta"">
                <p><strong>Want personalized guidance?</strong></p>
                <p>Book a call with Tim to discuss your results and get strategic recommendations tailored to your startup's unique situation.</p>
                <p><a href=""https://calendly.com/tim-startup"" class=""button"" style=""background: #764ba2;"">Book a Strategy Call</a></p>
            </div>
            
            <p style=""margin-top: 30px; font-style: italic; color: #666;"">
                Questions? We're here to help. Just reply to this email.
            </p>
        </div>
        
        <div class=""footer"">
            <p>© 2026 StartupAgent. All rights reserved.</p>
            <p><a href=""https://startupaigent.com"" style=""color: #667eea; text-decoration: none;"">Visit StartupAgent</a></p>
        </div>
    </div>
</body>
</html>
";
    }
}
