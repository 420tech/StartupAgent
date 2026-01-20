using System.Threading.Channels;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Simple in-memory job queue for deck analysis processing
/// </summary>
public interface IDeckAnalysisJobQueue
{
    /// <summary>
    /// Enqueue a deck analysis job
    /// </summary>
    ValueTask QueueJobAsync(string deckAnalysisId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue a job for processing
    /// </summary>
    ValueTask<string?> DequeueJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current queue depth
    /// </summary>
    int GetQueueDepth();
}

public class DeckAnalysisJobQueue : IDeckAnalysisJobQueue
{
    private readonly Channel<string> _queue;

    public DeckAnalysisJobQueue()
    {
        // Create unbounded channel for job queue
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        
        _queue = Channel.CreateUnbounded<string>(options);
        Console.WriteLine("DeckAnalysisJobQueue initialized");
    }

    public async ValueTask QueueJobAsync(string deckAnalysisId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(deckAnalysisId))
        {
            throw new ArgumentNullException(nameof(deckAnalysisId));
        }

        await _queue.Writer.WriteAsync(deckAnalysisId, cancellationToken);
        Console.WriteLine($"Queued deck analysis job: {deckAnalysisId}");
    }

    public async ValueTask<string?> DequeueJobAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deckAnalysisId = await _queue.Reader.ReadAsync(cancellationToken);
            Console.WriteLine($"Dequeued deck analysis job: {deckAnalysisId}");
            return deckAnalysisId;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public int GetQueueDepth()
    {
        return _queue.Reader.Count;
    }
}
