using StartupAgent.Server.Services.Jobs;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Background job service for detecting inactive sessions and triggering recovery flows.
/// Runs every 5 minutes to identify sessions that have been inactive for >15 minutes.
/// </summary>
public class SessionInactivityDetectionJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionInactivityDetectionJobService> _logger;
    private static readonly TimeSpan DetectionInterval = TimeSpan.FromMinutes(5);

    public SessionInactivityDetectionJobService(
        IServiceProvider serviceProvider,
        ILogger<SessionInactivityDetectionJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the inactivity detection job on a periodic schedule.
    /// Runs every 5 minutes to catch inactive sessions quickly.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionInactivityDetectionJobService is starting");

        using var timer = new PeriodicTimer(DetectionInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await DetectInactiveSessionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session inactivity detection job execution");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SessionInactivityDetectionJobService is stopping");
        }
    }

    /// <summary>
    /// Detects inactive sessions and records drop-off events.
    /// </summary>
    private async Task DetectInactiveSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dropOffService = scope.ServiceProvider.GetRequiredService<ISessionDropOffService>();

        try
        {
            await dropOffService.DetectInactiveSessions(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting inactive sessions");
        }
    }
}
