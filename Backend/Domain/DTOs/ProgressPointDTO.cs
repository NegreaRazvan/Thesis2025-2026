namespace Domain.DTOs;

public record ProgressPointDTO(
    DateTime Date,
    string PredictedCefr,
    float Mattr,
    float MedianZipf,
    float AvgDepDepth,
    float GrammarErrorRate,
    float CoherenceMean);
