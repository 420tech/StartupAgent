using StartupAgent.Shared.Models;
using StartupAgent.Shared.Models.Booking;

namespace StartupAgent.Shared.Services.Email;

/// <summary>
/// Service for sending deck analysis notifications (success/failure)
/// </summary>
public interface IDeckAnalysisNotificationService
{
    /// <summary>
    /// Send success notification when analysis completes
    /// </summary>
    Task<EmailSendResult> SendSuccessNotificationAsync(
        DeckAnalysisNotification notification,
        DeckAnalysis deckAnalysis,
        Founder founder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send failure notification when analysis fails
    /// </summary>
    Task<EmailSendResult> SendFailureNotificationAsync(
        DeckAnalysisNotification notification,
        DeckAnalysis deckAnalysis,
        Founder founder,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of deck analysis notification service
/// </summary>
public class DeckAnalysisNotificationService : IDeckAnalysisNotificationService
{
    private readonly ITemplateRenderer _renderer;

    public DeckAnalysisNotificationService(ITemplateRenderer renderer)
    {
        _renderer = renderer;
    }
    public async Task<EmailSendResult> SendSuccessNotificationAsync(
        DeckAnalysisNotification notification,
        DeckAnalysis deckAnalysis,
        Founder founder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine(
                $"[{notification.CorrelationId}] Sending deck analysis success notification to {founder.Email}");

            // Try template rendering first (success)
            var variables = new Dictionary<string, string>
            {
                ["founderName"] = !string.IsNullOrEmpty(founder.DisplayName) ? founder.DisplayName : founder.Email.Split('@')[0],
                ["completedTime"] = (deckAnalysis.CompletedAt?.ToString("MMM d, yyyy 'at' h:mm tt") ?? "Recently"),
                ["originalFileName"] = deckAnalysis.OriginalFileName ?? deckAnalysis.FileName
            };

            var rendered = await _renderer.RenderAsync(
                templateCode: "deck-analysis-results-email",
                language: EmailTemplateLanguage.English,
                variables: variables,
                useHtml: true,
                cancellationToken: cancellationToken);

            var htmlContent = string.IsNullOrWhiteSpace(rendered.Body)
                ? BuildSuccessEmailHtml(founder, deckAnalysis)
                : rendered.Body;

            // TODO: Replace with actual email service (SendGrid, AWS SES, etc.)
            // For now, simulate email send with delay
            await Task.Delay(1000, cancellationToken);

            Console.WriteLine(
                $"[{notification.CorrelationId}] Deck analysis success notification sent to {founder.Email}");

            return new EmailSendResult
            {
                Success = true,
                CorrelationId = notification.CorrelationId
            };
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                $"[{notification.CorrelationId}] Deck analysis notification send cancelled");

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Notification send cancelled",
                CanRetry = true,
                CorrelationId = notification.CorrelationId
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[{notification.CorrelationId}] Error sending deck analysis success notification: {ex.Message}");

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = $"Failed to send notification: {ex.Message}",
                CanRetry = true,
                CorrelationId = notification.CorrelationId
            };
        }
    }

    public async Task<EmailSendResult> SendFailureNotificationAsync(
        DeckAnalysisNotification notification,
        DeckAnalysis deckAnalysis,
        Founder founder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine(
                $"[{notification.CorrelationId}] Sending deck analysis failure notification to {founder.Email}");

            // Try template rendering first (failure)
            var variables = new Dictionary<string, string>
            {
                ["founderName"] = !string.IsNullOrEmpty(founder.DisplayName) ? founder.DisplayName : founder.Email.Split('@')[0],
                ["originalFileName"] = deckAnalysis.OriginalFileName ?? deckAnalysis.FileName,
                ["status"] = "Review needed"
            };

            var rendered = await _renderer.RenderAsync(
                templateCode: "deck-analysis-failure-email",
                language: EmailTemplateLanguage.English,
                variables: variables,
                useHtml: true,
                cancellationToken: cancellationToken);

            var htmlContent = string.IsNullOrWhiteSpace(rendered.Body)
                ? BuildFailureEmailHtml(founder, deckAnalysis)
                : rendered.Body;

            // TODO: Replace with actual email service (SendGrid, AWS SES, etc.)
            // For now, simulate email send with delay
            await Task.Delay(1000, cancellationToken);

            Console.WriteLine(
                $"[{notification.CorrelationId}] Deck analysis failure notification sent to {founder.Email}");

            return new EmailSendResult
            {
                Success = true,
                CorrelationId = notification.CorrelationId
            };
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                $"[{notification.CorrelationId}] Deck analysis failure notification send cancelled");

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Notification send cancelled",
                CanRetry = true,
                CorrelationId = notification.CorrelationId
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[{notification.CorrelationId}] Error sending deck analysis failure notification: {ex.Message}");

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = $"Failed to send notification: {ex.Message}",
                CanRetry = true,
                CorrelationId = notification.CorrelationId
            };
        }
    }

    /// <summary>
    /// Build HTML for success notification email
    /// </summary>
    private string BuildSuccessEmailHtml(Founder founder, DeckAnalysis deckAnalysis)
    {
        var displayName = !string.IsNullOrEmpty(founder.DisplayName)
            ? founder.DisplayName
            : founder.Email.Split('@')[0];

        var completedTime = deckAnalysis.CompletedAt?.ToString("MMM d, yyyy 'at' h:mm tt") ?? "Recently";

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
        .success-badge {{ display: inline-block; background: #28a745; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; margin: 10px 0; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }}
        .highlight {{ background: #e8f5e9; padding: 15px; border-left: 4px solid #4caf50; border-radius: 4px; margin: 20px 0; }}
        .footer {{ text-align: center; font-size: 12px; color: #666; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Your Deck Analysis is Ready! 🎉</h1>
        </div>
        <div class=""content"">
            <p>Hi {displayName},</p>
            
            <p>Great news! Your pitch deck has been analyzed and insights are ready.</p>
            
            <div class=""success-badge"">✓ Analysis Complete</div>
            
            <div class=""highlight"">
                <strong>Analysis completed at {completedTime}</strong><br>
                File: {deckAnalysis.OriginalFileName}
            </div>
            
            <p><strong>What's Next?</strong></p>
            <ul>
                <li>Log in to your StartupAgent dashboard to view full insights</li>
                <li>See TAM/GTM clarity, governance signals, and storytelling scores</li>
                <li>Identify red flags and recommended follow-ups</li>
            </ul>
            
            <a href=""https://startupaigent.com/dashboard"" class=""button"">View Your Insights</a>
            
            <p style=""margin-top: 30px;"">
                <strong>Want personalized guidance?</strong><br>
                Book a call with Tim to discuss your deck analysis and get strategic recommendations.
            </p>
            <a href=""https://calendly.com/tim-startup"" class=""button"" style=""background: #764ba2;"">Book a Strategy Call</a>
            
            <p style=""margin-top: 30px; font-style: italic; color: #666;"">
                Questions? We're here to help. Just reply to this email.
            </p>
        </div>
        
        <div class=""footer"">
            <p>© 2026 StartupAgent. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
    }

    /// <summary>
    /// Build HTML for failure notification email
    /// </summary>
    private string BuildFailureEmailHtml(Founder founder, DeckAnalysis deckAnalysis)
    {
        var displayName = !string.IsNullOrEmpty(founder.DisplayName)
            ? founder.DisplayName
            : founder.Email.Split('@')[0];

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .warning-badge {{ display: inline-block; background: #ff9800; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; margin: 10px 0; }}
        .button {{ display: inline-block; background: #ff6b6b; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }}
        .secondary-button {{ background: #667eea; }}
        .highlight {{ background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; border-radius: 4px; margin: 20px 0; }}
        .footer {{ text-align: center; font-size: 12px; color: #666; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Deck Analysis Update</h1>
        </div>
        <div class=""content"">
            <p>Hi {displayName},</p>
            
            <p>We encountered an issue while analyzing your pitch deck. This sometimes happens with complex files or specific formats.</p>
            
            <div class=""warning-badge"">⚠ Analysis Incomplete</div>
            
            <div class=""highlight"">
                <strong>File:</strong> {deckAnalysis.OriginalFileName}<br>
                <strong>Status:</strong> Review needed
            </div>
            
            <p><strong>Here's what you can do:</strong></p>
            <ul>
                <li><strong>Try again:</strong> Re-upload your deck file (PDF or PowerPoint)</li>
                <li><strong>Manual review:</strong> Book a call with Tim for personalized feedback on your deck</li>
                <li><strong>Contact support:</strong> Email us for help troubleshooting</li>
            </ul>
            
            <p>Don't worry—this is rare, and we're here to help!</p>
            
            <a href=""https://startupaigent.com/dashboard"" class=""button"">Try Again</a>
            
            <p style=""margin-top: 30px;"">
                <strong>Prefer personalized guidance?</strong><br>
                Book a call with Tim to discuss your deck directly. He can provide detailed feedback tailored to your startup's unique situation.
            </p>
            <a href=""https://calendly.com/tim-startup"" class=""button secondary-button"">Book a Strategy Call</a>
            
            <p style=""margin-top: 30px; font-style: italic; color: #666;"">
                Questions? We're here to help. Just reply to this email or contact support.
            </p>
        </div>
        
        <div class=""footer"">
            <p>© 2026 StartupAgent. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
    }
}
