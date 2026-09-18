namespace Domain.DTOs;

public record SubmissionSummaryDTO
{
    public Guid Id { get; init; }
    public string TextContent { get; init; } = "";
    public string PredictedCefr { get; init; } = "";
    public int TokenCount { get; init; }
    public DateTime SubmittedAt { get; init; }
}
