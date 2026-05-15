using SummaryService.Domain.Enums;

namespace SummaryService.Domain.ValueObjects;

public sealed record SummaryOptions(
    int MaxTokens,
    double Temperature,
    SummaryStyle Style);