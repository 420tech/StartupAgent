using StartupAgent.Data;
using StartupAgent.Shared.Models;
using StartupAgent.Shared.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Background service for processing deck analysis notifications with retry logic
/// </summary>
public class DeckAnalysisNotificationProcessor : BackgroundService
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _serviceProvider;
    private readonly IDeckAnalysisNotificationQueue _notificationQueue;
    private readonly ILogger<DeckAnalysisNotificationProcessor> _logger;

    public DeckAnalysisNotificationProcessor(
        IServiceProvider serviceProvider,
        IDeckAnalysisNotificationQueue notificationQueue,
        ILogger<DeckAnalysisNotificationProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    /// <summary>
    /// Main processing loop
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deck analysis notification processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var notificationId = await _notificationQueue.DequeueJobAsync(stoppingToken);
                if (notificationId != null)
                {
                    await ProcessNotificationAsync(notificationId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Deck analysis notification processor is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in deck analysis notification processor");
                // Small delay before retrying to avoid tight loop on errors
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Deck analysis notification processor stopped");
    }

    /// <summary>
    /// Process individual notification
    /// </summary>
    private async Task ProcessNotificationAsync(string notificationId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IDeckAnalysisNotificationService>();

            // Load notification record
            var notification = await context.DeckAnalysisNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null)
            {
                _logger.LogWarning("Deck analysis notification {NotificationId} not found", notificationId);
                return;
            }

            // Skip if already sent or failed permanently
            if (notification.Status == DeckAnalysisNotificationStatus.Sent ||
                (notification.Status == DeckAnalysisNotificationStatus.Failed &&
                 notification.AttemptCount >= MaxRetries))
            {
                _logger.LogInformation(
                    "Skipping notification {NotificationId} with status {Status}",
                    notificationId,
                    notification.Status);
                return;
            }

            // Load deck analysis
            var deckAnalysis = await context.DeckAnalyses
                .FirstOrDefaultAsync(d => d.Id == notification.DeckAnalysisId, cancellationToken);

            if (deckAnalysis == null)
            {
                _logger.LogWarning(
                    "Deck analysis {DeckAnalysisId} not found for notification {NotificationId}",
                    notification.DeckAnalysisId,
                    notificationId);
                await HandleNotificationFailureAsync(
                    context,
                    notification,
                    "Deck analysis not found",
                    cancellationToken);
                return;
            }

            // Load founder for personalization
            var founder = await context.Founders
                .FirstOrDefaultAsync(f => f.Id == notification.FounderId, cancellationToken);

            if (founder == null)
            {
                _logger.LogWarning(
                    "Founder {FounderId} not found for notification {NotificationId}",
                    notification.FounderId,
                    notificationId);
                await HandleNotificationFailureAsync(
                    context,
                    notification,
                    "Founder not found",
                    cancellationToken);
                return;
            }

            // Update status to sending
            notification.Status = DeckAnalysisNotificationStatus.Sending;
            notification.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[{CorrelationId}] Processing {NotificationType} notification {NotificationId} for deck {DeckAnalysisId}",
                notification.CorrelationId,
                notification.NotificationType,
                notificationId,
                notification.DeckAnalysisId);

            // Send notification based on type
            var result = notification.NotificationType switch
            {
                DeckAnalysisNotificationType.Success =>
                    await notificationService.SendSuccessNotificationAsync(notification, deckAnalysis, founder, cancellationToken),
                DeckAnalysisNotificationType.Failure =>
                    await notificationService.SendFailureNotificationAsync(notification, deckAnalysis, founder, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown notification type: {notification.NotificationType}")
            };

            if (result.Success)
            {
                // Mark as sent
                notification.Status = DeckAnalysisNotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
                notification.AttemptCount++;

                _logger.LogInformation(
                    "[{CorrelationId}] Notification {NotificationId} sent successfully",
                    notification.CorrelationId,
                    notificationId);
            }
            else
            {
                // Notification send failed
                throw new InvalidOperationException(result.ErrorMessage ?? "Unknown notification send error");
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing notification {NotificationId}: {Error}",
                notificationId,
                ex.Message);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var notification = await context.DeckAnalysisNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification != null)
            {
                await HandleNotificationFailureAsync(
                    context,
                    notification,
                    ex.Message,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handle notification failure with retry logic
    /// </summary>
    private async Task HandleNotificationFailureAsync(
        ApplicationDbContext context,
        DeckAnalysisNotification notification,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        notification.AttemptCount++;
        notification.LastError = errorMessage;
        notification.UpdatedAt = DateTime.UtcNow;

        if (notification.AttemptCount < MaxRetries)
        {
            notification.Status = DeckAnalysisNotificationStatus.Pending;

            _logger.LogInformation(
                "[{CorrelationId}] Notification {NotificationId} will retry. Attempt {Attempt}/{MaxAttempts}",
                notification.CorrelationId,
                notification.Id,
                notification.AttemptCount,
                MaxRetries);

            await context.SaveChangesAsync(cancellationToken);

            // Re-queue after delay
            await Task.Delay(RetryDelay, cancellationToken);
            await _notificationQueue.QueueJobAsync(notification.Id, cancellationToken);
        }
        else
        {
            notification.Status = DeckAnalysisNotificationStatus.Failed;

            _logger.LogError(
                "[{CorrelationId}] Notification {NotificationId} failed after {MaxAttempts} attempts: {Error}",
                notification.CorrelationId,
                notification.Id,
                MaxRetries,
                errorMessage);

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
