using System.Threading.Channels;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Interface for deck analysis notification queue
/// </summary>
public interface IDeckAnalysisNotificationQueue
{
    /// <summary>
    /// Queue a deck analysis notification for sending
    /// </summary>
    ValueTask QueueJobAsync(
        string notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue next pending notification
    /// </summary>
    ValueTask<string?> DequeueJobAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get current queue depth
    /// </summary>
    int GetQueueDepth();
}

/// <summary>
/// Implementation of deck analysis notification queue using System.Threading.Channels
/// </summary>
public class DeckAnalysisNotificationQueue : IDeckAnalysisNotificationQueue
{
    private readonly Channel<string> _queue;
    private readonly ILogger<DeckAnalysisNotificationQueue> _logger;

    public DeckAnalysisNotificationQueue(ILogger<DeckAnalysisNotificationQueue> logger)
    {
        _logger = logger;

        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _queue = Channel.CreateUnbounded<string>(options);
    }

    /// <summary>
    /// Queue notification for sending
    /// </summary>
    public async ValueTask QueueJobAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _queue.Writer.WriteAsync(notificationId, cancellationToken);
            _logger.LogInformation(
                "Deck analysis notification {NotificationId} queued. Queue depth: {Depth}",
                notificationId,
                GetQueueDepth());
        }
        catch (ChannelClosedException)
        {
            _logger.LogError(
                "Cannot queue notification {NotificationId}: queue is closed",
                notificationId);
            throw;
        }
    }

    /// <summary>
    /// Dequeue next notification for processing
    /// </summary>
    public async ValueTask<string?> DequeueJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await _queue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_queue.Reader.TryRead(out var notificationId))
                {
                    _logger.LogInformation(
                        "Deck analysis notification {NotificationId} dequeued for processing",
                        notificationId);
                    return notificationId;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Deck analysis notification queue dequeue cancelled");
        }

        return null;
    }

    /// <summary>
    /// Get current queue depth
    /// </summary>
    public int GetQueueDepth()
    {
        return _queue.Reader.Count;
    }
}
