namespace Domain.DTOs;

public record SubmissionResponseDTO
{
    public Guid Id { get; init; }
    public string TextContent { get; init; } = "";
    public string PredictedCefr { get; init; } = "";
    public Dictionary<string, float> Confidence { get; init; } = [];
    public string AiFeedback { get; init; } = "";
    public FeatureSnapshotDTO Features { get; init; } = new();
    public List<ErrorItemDTO> Errors { get; init; } = [];
    public List<VocabCandidateDTO> VocabCandidates { get; init; } = [];
    public bool ShortTextWarning { get; init; }
    public DateTime SubmittedAt { get; init; }
}
