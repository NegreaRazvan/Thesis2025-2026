namespace Domain.DTOs;

/// <summary>Returned by POST /api/Transcription after Whisper processes the audio.</summary>
public record TranscriptionResultDTO(string Text, string Language, float DurationSeconds);