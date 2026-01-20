using System.Threading.Channels;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Interface for recovery email job queue
/// </summary>
public interface IRecoveryEmailJobQueue
{
    /// <summary>
    /// Queue a recovery email for sending
    /// </summary>
    ValueTask QueueJobAsync(
        string recoveryEmailId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue next pending recovery email
    /// </summary>
    ValueTask<string?> DequeueJobAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get current queue depth
    /// </summary>
    int GetQueueDepth();
}

/// <summary>
/// Implementation of recovery email job queue using System.Threading.Channels
/// </summary>
public class RecoveryEmailJobQueue : IRecoveryEmailJobQueue
{
    private readonly Channel<string> _queue;
    private readonly ILogger<RecoveryEmailJobQueue> _logger;

    public RecoveryEmailJobQueue(ILogger<RecoveryEmailJobQueue> logger)
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
    /// Queue recovery email for sending
    /// </summary>
    public async ValueTask QueueJobAsync(
        string recoveryEmailId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _queue.Writer.WriteAsync(recoveryEmailId, cancellationToken);
            _logger.LogInformation(
                "Recovery email {RecoveryEmailId} queued for sending. Queue depth: {Depth}",
                recoveryEmailId,
                GetQueueDepth());
        }
        catch (ChannelClosedException)
        {
            _logger.LogError(
                "Cannot queue recovery email {RecoveryEmailId}: queue is closed",
                recoveryEmailId);
            throw;
        }
    }

    /// <summary>
    /// Dequeue next recovery email for processing
    /// </summary>
    public async ValueTask<string?> DequeueJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await _queue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_queue.Reader.TryRead(out var recoveryEmailId))
                {
                    _logger.LogInformation(
                        "Recovery email {RecoveryEmailId} dequeued for processing",
                        recoveryEmailId);
                    return recoveryEmailId;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recovery email queue dequeue cancelled");
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
