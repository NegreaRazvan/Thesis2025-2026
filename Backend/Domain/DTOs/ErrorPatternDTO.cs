namespace Domain.DTOs;

public record ErrorPatternDTO(
    string ErrorCategory,
    string RuleId,
    int Count,
    DateTime LastSeen);
