using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using StartupAgent.Data;
using StartupAgent.Server.Services.Analysis;
using StartupAgent.Server.Services.Jobs;
using StartupAgent.Shared.Models;
using System.Text.Json;

namespace StartupAgent.Server.Services.Jobs;

/// <summary>
/// Background service that processes deck analysis jobs
/// </summary>
public class DeckAnalysisJobProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeckAnalysisJobQueue _jobQueue;
    private readonly ILogger<DeckAnalysisJobProcessor> _logger;
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(2);

    public DeckAnalysisJobProcessor(
        IServiceProvider serviceProvider,
        IDeckAnalysisJobQueue jobQueue,
        ILogger<DeckAnalysisJobProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeckAnalysisJobProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var deckAnalysisId = await _jobQueue.DequeueJobAsync(stoppingToken);
                
                if (deckAnalysisId != null)
                {
                    await ProcessJobAsync(deckAnalysisId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job processor loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("DeckAnalysisJobProcessor stopped");
    }

    private async Task ProcessJobAsync(string deckAnalysisId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var analysisService = scope.ServiceProvider.GetRequiredService<IDeckAnalysisService>();

        try
        {
            _logger.LogInformation("Processing deck analysis job: {DeckAnalysisId}", deckAnalysisId);

            // Load DeckAnalysis record
            var deckAnalysis = await context.DeckAnalyses
                .FirstOrDefaultAsync(d => d.Id == deckAnalysisId, cancellationToken);

            if (deckAnalysis == null)
            {
                _logger.LogWarning("DeckAnalysis not found: {DeckAnalysisId}", deckAnalysisId);
                return;
            }

            // Update status to Running
            deckAnalysis.Status = ReportStatus.Running;
            deckAnalysis.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // Perform analysis
            var insights = await analysisService.AnalyzeDeckAsync(deckAnalysis.FileUrl, cancellationToken);

            // Serialize insights to JSON
            var insightsJson = JsonSerializer.Serialize(insights, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Update record with success
            deckAnalysis.InsightsJson = insightsJson;
            deckAnalysis.Status = ReportStatus.Succeeded;
            deckAnalysis.CompletedAt = DateTime.UtcNow;
            deckAnalysis.UpdatedAt = DateTime.UtcNow;
            deckAnalysis.ErrorMessage = null;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deck analysis succeeded: {DeckAnalysisId}", deckAnalysisId);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Deck analysis timed out: {DeckAnalysisId}", deckAnalysisId);
            await HandleJobFailureAsync(context, deckAnalysisId, "Analysis timeout", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deck analysis failed: {DeckAnalysisId}", deckAnalysisId);
            await HandleJobFailureAsync(context, deckAnalysisId, ex.Message, cancellationToken);
        }
    }

    private async Task HandleJobFailureAsync(
        ApplicationDbContext context,
        string deckAnalysisId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var deckAnalysis = await context.DeckAnalyses
            .FirstOrDefaultAsync(d => d.Id == deckAnalysisId, cancellationToken);

        if (deckAnalysis == null)
        {
            return;
        }

        deckAnalysis.RetryCount++;
        deckAnalysis.ErrorMessage = errorMessage;
        deckAnalysis.UpdatedAt = DateTime.UtcNow;

        if (deckAnalysis.RetryCount < MaxRetries)
        {
            // Retry the job
            deckAnalysis.Status = ReportStatus.Retrying;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Retrying deck analysis (attempt {RetryCount}/{MaxRetries}): {DeckAnalysisId}",
                deckAnalysis.RetryCount + 1,
                MaxRetries,
                deckAnalysisId);

            // Re-queue with delay
            await Task.Delay(RetryDelay, cancellationToken);
            await _jobQueue.QueueJobAsync(deckAnalysisId, cancellationToken);
        }
        else
        {
            // Max retries exceeded
            deckAnalysis.Status = ReportStatus.Failed;
            deckAnalysis.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                "Deck analysis failed after {MaxRetries} retries: {DeckAnalysisId}",
                MaxRetries,
                deckAnalysisId);
        }
    }
}
