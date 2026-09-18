using Domain.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Service.Services;

internal static class ProgressReportPdfBuilder
{
    internal static byte[] Build(
        List<ProgressPointDTO> timeline,
        List<ErrorPatternDTO> patterns,
        string username)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var latestCefr = timeline.Count > 0 ? timeline[^1].PredictedCefr : "—";
        var bestCefr   = timeline.Count > 0
            ? timeline.OrderByDescending(t => CefrRank(t.PredictedCefr)).First().PredictedCefr
            : "—";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Text("LinguaForge — Progress Report")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"User: {username}").FontSize(10).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Background(Colors.Blue.Lighten5).Padding(14).Column(summary =>
                    {
                        summary.Item().Text("Summary").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                        summary.Item().PaddingTop(6).Row(row =>
                        {
                            SummaryCell(row, "Total submissions", timeline.Count.ToString());
                            SummaryCell(row, "Current level", latestCefr);
                            SummaryCell(row, "Best level", bestCefr);
                        });
                    });

                    col.Item().PaddingTop(16);

                    if (timeline.Count > 0)
                    {
                        col.Item().Text("Submission History").FontSize(13).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                TableHeaderCell(header.Cell(), "Date");
                                TableHeaderCell(header.Cell(), "CEFR");
                                TableHeaderCell(header.Cell(), "MATTR %");
                                TableHeaderCell(header.Cell(), "Error rate");
                            });

                            foreach (var row in timeline)
                            {
                                TableCell(table, row.Date.ToString("dd MMM yyyy"));
                                TableCell(table, row.PredictedCefr);
                                TableCell(table, $"{row.Mattr * 100:F1}%");
                                TableCell(table, $"{row.GrammarErrorRate:F2}");
                            }
                        });
                    }

                    col.Item().PaddingTop(16);

                    if (patterns.Count > 0)
                    {
                        col.Item().Text("Recurring Error Patterns").FontSize(13).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                TableHeaderCell(header.Cell(), "Category");
                                TableHeaderCell(header.Cell(), "Rule");
                                TableHeaderCell(header.Cell(), "Count");
                            });

                            foreach (var p in patterns)
                            {
                                TableCell(table, p.ErrorCategory);
                                TableCell(table, p.RuleId);
                                TableCell(table, p.Count.ToString());
                            }
                        });
                    }
                });

                page.Footer().AlignCenter()
                    .Text("Generated by LinguaForge").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    private static int CefrRank(string cefr) => cefr switch
    {
        "A1" => 1, "A2" => 2, "B1" => 3, "B2" => 4, "C1" => 5, _ => 0
    };

    private static void SummaryCell(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Medium);
            col.Item().Text(value).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
        });
    }

    private static void TableHeaderCell(IContainer container, string text)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
            .Padding(5).Text(text).Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
    }

    private static void TableCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .Padding(5).Text(text).FontSize(9);
    }
}
