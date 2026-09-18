namespace Repository.EFEntities;

public class SubmissionError
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public required string ErrorCategory { get; set; }
    public string? RuleId { get; set; }
    public required string Message { get; set; }
    public int OffsetStart { get; set; }
    public int OffsetEnd { get; set; }
    public string? BadText { get; set; }
    public string SuggestionsJson { get; set; } = "[]";
    public string Lemma { get; set; } = "";
    public string EnglishTranslation { get; set; } = "";
    public string Article { get; set; } = "";
    public string Plural { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}