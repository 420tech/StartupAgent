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
    private readonly ITemplateRenderer _renderer;

    public RecoveryEmailService(ITemplateRenderer renderer)
    {
        _renderer = renderer;
    }

    public async Task<EmailSendResult> SendRecoveryEmailAsync(
        RecoveryEmail recoveryEmail,
        Founder founder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine(
                $"Sending recovery email to founder {founder.Id} at {recoveryEmail.Email}");

            // Render from template system
            var variables = new Dictionary<string, string>
            {
                ["founderName"] = !string.IsNullOrEmpty(founder.DisplayName) ? founder.DisplayName : founder.Email.Split('@')[0],
                ["resumeLink"] = recoveryEmail.ResumeLink
            };

            var rendered = await _renderer.RenderAsync(
                templateCode: "session-recovery-email",
                language: EmailTemplateLanguage.English,
                variables: variables,
                useHtml: true,
                cancellationToken: cancellationToken);

            // TODO: Replace with actual email service (SendGrid, AWS SES, etc.)
            // For now, simulate email send with delay
            await Task.Delay(1000, cancellationToken);

            Console.WriteLine(
                $"Recovery email sent successfully to {recoveryEmail.Email} (Template={rendered.TemplateId}, Version={rendered.Version})");

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
}
