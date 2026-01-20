using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StartupAgent.Shared.Models.Scoring;

namespace StartupAgent.Shared.Services.Pdf;

/// <summary>
/// Service to generate branded PDF roadmaps from diagnostic results
/// </summary>
public interface IRoadmapPdfService
{
    /// <summary>
    /// Generate a PDF roadmap from diagnostic results
    /// </summary>
    Task<byte[]> GeneratePdfAsync(
        DiagnosticResults results,
        CancellationToken cancellationToken = default);
}

public class RoadmapPdfService : IRoadmapPdfService
{
    public async Task<byte[]> GeneratePdfAsync(
        DiagnosticResults results,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        // Configure QuestPDF to use Community license for development
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(50);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header()
                    .AlignCenter()
                    .Column(column =>
                    {
                        column.Item().Text("StartupAgent")
                            .FontSize(24)
                            .Bold()
                            .FontColor("#0EA5E9");
                        
                        column.Item().Text("90-Day Software Readiness Roadmap")
                            .FontSize(16)
                            .FontColor("#64748B");
                        
                        column.Item().PaddingTop(10).Text($"Generated: {DateTime.UtcNow:MMMM dd, yyyy}")
                            .FontSize(10)
                            .FontColor("#94A3B8");
                    });

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        // Overall Score Section
                        column.Item().PaddingBottom(20).Column(section =>
                        {
                            section.Item().Text("Your Overall Readiness Score")
                                .FontSize(18)
                                .Bold()
                                .FontColor("#1E293B");
                            
                            section.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"{results.OverallScore}/100")
                                    .FontSize(36)
                                    .Bold()
                                    .FontColor(GetStatusColor(results.OverallStatus));
                                
                                row.RelativeItem().AlignRight().Text(results.OverallStatus)
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(GetStatusColor(results.OverallStatus));
                            });
                        });

                        // Personal Narrative Section
                        if (!string.IsNullOrEmpty(results.Narrative))
                        {
                            column.Item().PaddingBottom(20).Column(section =>
                            {
                                section.Item().Text("Personal Guidance from Tim")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor("#1E293B");
                                
                                section.Item()
                                    .PaddingTop(10)
                                    .Background("#F1F5F9")
                                    .Padding(15)
                                    .Text(results.Narrative)
                                    .FontSize(11)
                                    .LineHeight(1.6f)
                                    .FontColor("#334155");
                            });
                        }

                        // Top Priorities Section
                        if (results.TopPriorities.Any())
                        {
                            column.Item().PaddingBottom(20).Column(section =>
                            {
                                section.Item().Text("Your Top Priorities (Next 90 Days)")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor("#1E293B");
                                
                                section.Item().PaddingTop(10).Column(list =>
                                {
                                    foreach (var (priority, index) in results.TopPriorities.Select((p, i) => (p, i)))
                                    {
                                        list.Item().PaddingBottom(8).Row(row =>
                                        {
                                            row.ConstantItem(25).Text($"{index + 1}.")
                                                .Bold()
                                                .FontColor("#0EA5E9");
                                            
                                            row.RelativeItem().Text(priority)
                                                .FontSize(11)
                                                .FontColor("#334155");
                                        });
                                    }
                                });
                            });
                        }

                        // Dimension Scores Section
                        column.Item().PaddingBottom(20).Column(section =>
                        {
                            section.Item().Text("Dimension Breakdown")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1E293B");
                            
                            section.Item().PaddingTop(10).Column(list =>
                            {
                                foreach (var dimension in results.DimensionScores)
                                {
                                    list.Item().PaddingBottom(12).Column(dimSection =>
                                    {
                                        dimSection.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(dimension.DimensionName)
                                                .FontSize(12)
                                                .Bold()
                                                .FontColor("#1E293B");
                                            
                                            row.ConstantItem(80).AlignRight().Text($"{dimension.Score}/100")
                                                .FontSize(11)
                                                .Bold()
                                                .FontColor(GetStatusColor(dimension.Status));
                                            
                                            row.ConstantItem(60).AlignRight().Text(dimension.Status)
                                                .FontSize(10)
                                                .FontColor(GetStatusColor(dimension.Status));
                                        });
                                        
                                        // Progress bar
                                        dimSection.Item().PaddingTop(5).Container().Height(8).Background("#E2E8F0").Row(row =>
                                        {
                                            row.RelativeItem(dimension.Score).Background(GetStatusColor(dimension.Status));
                                            row.RelativeItem(100 - dimension.Score).Background("#E2E8F0");
                                        });
                                        
                                        // Key insights
                                        if (dimension.RisksAndOpportunities.Any())
                                        {
                                            dimSection.Item().PaddingTop(5).Column(insights =>
                                            {
                                                foreach (var insight in dimension.RisksAndOpportunities.Take(2))
                                                {
                                                    insights.Item().PaddingLeft(10).Row(row =>
                                                    {
                                                        row.ConstantItem(15).Text("•")
                                                            .FontSize(10)
                                                            .FontColor("#64748B");
                                                        
                                                        row.RelativeItem().Text(insight)
                                                            .FontSize(9)
                                                            .FontColor("#64748B")
                                                            .LineHeight(1.4f);
                                                    });
                                                }
                                            });
                                        }
                                    });
                                }
                            });
                        });

                        // Next Steps Section
                        column.Item().Column(section =>
                        {
                            section.Item().Text("Next Steps")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1E293B");
                            
                            section.Item().PaddingTop(10).Column(list =>
                            {
                                list.Item().PaddingBottom(5).Text("1. Focus on your top 3 priorities listed above")
                                    .FontSize(10)
                                    .FontColor("#334155");
                                
                                list.Item().PaddingBottom(5).Text("2. Address Red dimension areas within the next 30 days")
                                    .FontSize(10)
                                    .FontColor("#334155");
                                
                                list.Item().PaddingBottom(5).Text("3. Review Yellow dimensions and create action plans")
                                    .FontSize(10)
                                    .FontColor("#334155");
                                
                                list.Item().PaddingBottom(5).Text("4. Book a strategy call with Tim to discuss your roadmap")
                                    .FontSize(10)
                                    .FontColor("#334155");
                            });
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Generated by ");
                        text.Span("StartupAgent").Bold().FontColor("#0EA5E9");
                        text.Span(" • ");
                        text.Span("TB Software Readiness Framework™");
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static string GetStatusColor(string status)
    {
        return status switch
        {
            "Green" => "#10B981",
            "Yellow" => "#F59E0B",
            "Red" => "#EF4444",
            _ => "#64748B"
        };
    }
}
