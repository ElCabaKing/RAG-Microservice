namespace SummaryService.Domain.ValueObjects;

public sealed record TokenLimits(
    int MaxInputTokens,
    int MaxOutputTokens);