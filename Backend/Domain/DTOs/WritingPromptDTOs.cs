namespace Domain.DTOs;

public record WritingPromptDTO
{
    public Guid Id { get; init; }
    public string CefrLevel { get; init; } = "";
    public string PromptDe { get; init; } = "";
    public string PromptEn { get; init; } = "";
    public string? TopicTag { get; init; }
}