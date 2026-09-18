namespace Domain.DTOs;

public record FeatureSnapshotDTO
{
    public float NTokens { get; init; }
    public float Mattr { get; init; }
    public float MedianZipf { get; init; }
    public float AvgDepDepth { get; init; }
    public float GrammarErrorRate { get; init; }
    public float CoherenceMean { get; init; }
    public float VocabSyntaxInteraction { get; init; }
    public float LexicalSophistication { get; init; }
}
