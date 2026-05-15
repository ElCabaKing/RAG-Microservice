namespace SummaryService.Domain.ValueObjects;

public sealed record ChunkData(
    int Index,
    string Content);