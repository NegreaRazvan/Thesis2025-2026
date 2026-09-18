namespace Repository.EFEntities;

public class WritingPrompt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CefrLevel { get; set; }
    public required string PromptDe { get; set; }
    public required string PromptEn { get; set; }
    public string? TopicTag { get; set; }
}
