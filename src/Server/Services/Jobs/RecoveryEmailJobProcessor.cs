using StartupAgent.Data;
using StartupAgent.Shared.Models;
using StartupAgent.Shared.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Background service for processing recovery emails with retry logic
/// </summary>
public class RecoveryEmailJobProcessor : BackgroundService
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _serviceProvider;
    private readonly IRecoveryEmailJobQueue _jobQueue;
    private readonly ILogger<RecoveryEmailJobProcessor> _logger;

    public RecoveryEmailJobProcessor(
        IServiceProvider serviceProvider,
        IRecoveryEmailJobQueue jobQueue,
        ILogger<RecoveryEmailJobProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    /// <summary>
    /// Main processing loop
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recovery email processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var recoveryEmailId = await _jobQueue.DequeueJobAsync(stoppingToken);
                if (recoveryEmailId != null)
                {
                    await ProcessJobAsync(recoveryEmailId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recovery email processor is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in recovery email processor");
                // Small delay before retrying to avoid tight loop on errors
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Recovery email processor stopped");
    }

    /// <summary>
    /// Process individual recovery email job
    /// </summary>
    private async Task ProcessJobAsync(string recoveryEmailId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IRecoveryEmailService>();

            // Load recovery email record
            var recoveryEmail = await context.RecoveryEmails
                .FirstOrDefaultAsync(r => r.Id == recoveryEmailId, cancellationToken);

            if (recoveryEmail == null)
            {
                _logger.LogWarning("Recovery email {RecoveryEmailId} not found", recoveryEmailId);
                return;
            }

            // Skip if already sent or failed permanently
            if (recoveryEmail.Status == RecoveryEmailSendStatus.Sent ||
                (recoveryEmail.Status == RecoveryEmailSendStatus.Failed &&
                 recoveryEmail.AttemptCount >= MaxRetries))
            {
                _logger.LogInformation(
                    "Skipping recovery email {RecoveryEmailId} with status {Status}",
                    recoveryEmailId,
                    recoveryEmail.Status);
                return;
            }

            // Load founder for personalization
            var founder = await context.Founders
                .FirstOrDefaultAsync(f => f.Id == recoveryEmail.FounderId, cancellationToken);

            if (founder == null)
            {
                _logger.LogWarning(
                    "Founder {FounderId} not found for recovery email {RecoveryEmailId}",
                    recoveryEmail.FounderId,
                    recoveryEmailId);
                await HandleJobFailureAsync(
                    context,
                    recoveryEmail,
                    "Founder not found",
                    cancellationToken);
                return;
            }

            // Update status to sending
            recoveryEmail.Status = RecoveryEmailSendStatus.Sending;
            recoveryEmail.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Processing recovery email {RecoveryEmailId} for founder {FounderId}",
                recoveryEmailId,
                founder.Id);

            // Send email
            var result = await emailService.SendRecoveryEmailAsync(recoveryEmail, founder, cancellationToken);

            if (result.Success)
            {
                // Mark as sent
                recoveryEmail.Status = RecoveryEmailSendStatus.Sent;
                recoveryEmail.SentAt = DateTime.UtcNow;
                recoveryEmail.UpdatedAt = DateTime.UtcNow;
                recoveryEmail.AttemptCount++;

                _logger.LogInformation(
                    "Recovery email {RecoveryEmailId} sent successfully",
                    recoveryEmailId);
            }
            else
            {
                // Email send failed
                throw new InvalidOperationException(result.ErrorMessage ?? "Unknown email send error");
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing recovery email {RecoveryEmailId}: {Error}",
                recoveryEmailId,
                ex.Message);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var recoveryEmail = await context.RecoveryEmails
                .FirstOrDefaultAsync(r => r.Id == recoveryEmailId, cancellationToken);

            if (recoveryEmail != null)
            {
                await HandleJobFailureAsync(
                    context,
                    recoveryEmail,
                    ex.Message,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handle job failure with retry logic
    /// </summary>
    private async Task HandleJobFailureAsync(
        ApplicationDbContext context,
        RecoveryEmail recoveryEmail,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        recoveryEmail.AttemptCount++;
        recoveryEmail.LastError = errorMessage;
        recoveryEmail.UpdatedAt = DateTime.UtcNow;

        if (recoveryEmail.AttemptCount < MaxRetries)
        {
            recoveryEmail.Status = RecoveryEmailSendStatus.Pending;

            _logger.LogInformation(
                "Recovery email {RecoveryEmailId} will retry. Attempt {Attempt}/{MaxAttempts}",
                recoveryEmail.Id,
                recoveryEmail.AttemptCount,
                MaxRetries);

            await context.SaveChangesAsync(cancellationToken);

            // Re-queue after delay
            await Task.Delay(RetryDelay, cancellationToken);
            await _jobQueue.QueueJobAsync(recoveryEmail.Id, cancellationToken);
        }
        else
        {
            recoveryEmail.Status = RecoveryEmailSendStatus.Failed;

            _logger.LogError(
                "Recovery email {RecoveryEmailId} failed after {MaxAttempts} attempts",
                recoveryEmail.Id,
                MaxRetries);

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
