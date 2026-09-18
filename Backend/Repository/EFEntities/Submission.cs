namespace Repository.EFEntities;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public required string TextContent { get; set; }
    public required string PredictedCefr { get; set; }
    public string ConfidenceJson { get; set; } = "{}";
    public string? AiFeedback { get; set; }

    // Feature snapshot — stored for progress charts
    public float NTokens { get; set; }
    public float Mattr { get; set; }
    public float MedianZipf { get; set; }
    public float AvgDepDepth { get; set; }
    public float GrammarErrorRate { get; set; }
    public float CoherenceMean { get; set; }
    public float VocabSyntaxInteraction { get; set; }
    public float LexicalSophistication { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public List<SubmissionError> Errors { get; set; } = [];
}
