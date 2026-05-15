using SummaryService.Domain.Enums;

namespace SummaryService.Domain.ValueObjects;

public sealed record DocumentContent(
    string Content,
    DocumentType Type,
    long SizeInBytes);