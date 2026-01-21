using Microsoft.EntityFrameworkCore;
using StartupAgent.Data;
using StartupAgent.Shared.Models;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Background job service for cleaning up incomplete sessions after 24 hours.
/// Sessions with Pending status older than 24 hours are deleted to prevent storage bloat.
/// </summary>
public class SessionCleanupJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupJobService> _logger;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan SessionRetentionPeriod = TimeSpan.FromHours(24);

    public SessionCleanupJobService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the cleanup job on a periodic schedule.
    /// Runs every hour to delete sessions older than 24 hours.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupJobService is starting");

        using var timer = new PeriodicTimer(CleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CleanupExpiredSessionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session cleanup job execution");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SessionCleanupJobService is stopping");
        }
    }

    /// <summary>
    /// Deletes sessions that have been paused (Paused status) for more than 24 hours.
    /// </summary>
    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoffTime = DateTime.UtcNow.Subtract(SessionRetentionPeriod);

        // Find sessions to delete: Paused status + created before cutoff time
        var sessionsToDelete = await context.Sessions
            .Where(s => s.Status == SessionStatus.Paused && s.CreatedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (sessionsToDelete.Count == 0)
        {
            _logger.LogDebug("No sessions to clean up");
            return;
        }

        context.Sessions.RemoveRange(sessionsToDelete);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SessionCleanupJobService deleted {Count} incomplete sessions older than {Hours} hours",
            sessionsToDelete.Count,
            (int)SessionRetentionPeriod.TotalHours);
    }
}
