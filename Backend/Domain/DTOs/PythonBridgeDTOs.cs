namespace Domain.DTOs;

// Internal DTOs used only by PythonMlService — never exposed to the frontend

public record PythonPredictRequestDTO(string Text);

public class PythonPredictResponseDTO
{
    public string PredictedLevel { get; set; } = "";
    public Dictionary<string, float> Confidence { get; set; } = new();
    public Dictionary<string, float> Features { get; set; } = new();
    public List<PythonErrorItemDTO> Errors { get; set; } = new();
    public bool ShortTextWarning { get; set; }

    /// <summary>
    /// GermanBERT-detected vocabulary candidates — words the user may have
    /// chosen incorrectly or doesn't know well enough.
    /// Empty list if detection is disabled or the text is too short.
    /// </summary>
    public List<PythonVocabCandidateDTO> VocabCandidates { get; set; } = new();
}

public class PythonErrorItemDTO
{
    public string Category { get; set; } = "";
    public string RuleId { get; set; } = "";
    public string Message { get; set; } = "";
    public int OffsetStart { get; set; }
    public int OffsetEnd { get; set; }
    public string BadText { get; set; } = "";
    public List<string> Suggestions { get; set; } = new();
    // Populated only for spelling errors by Python (spaCy lemma + Claude translation)
    public string Lemma { get; set; } = "";
    public string EnglishTranslation { get; set; } = "";
    public string Article { get; set; } = "";
    public string Plural { get; set; } = "";
}

/// <summary>
/// A word flagged by GermanBERT as a potential vocabulary error or unknown word.
/// </summary>
public class PythonVocabCandidateDTO
{
    public string Token { get; set; } = "";
    public string Lemma { get; set; } = "";
    public string Pos { get; set; } = "";
    public int OffsetStart { get; set; }
    public int OffsetEnd { get; set; }
    public float ActualProbability { get; set; }
    public List<string> TopAlternatives { get; set; } = new();
    public float ZipfScore { get; set; }
    public string Reason { get; set; } = "";
    public string FlashcardFront { get; set; } = "";
    public string FlashcardBack { get; set; } = "";
}