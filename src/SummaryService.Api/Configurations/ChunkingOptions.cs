namespace SummaryService.Api.Configurations;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public int ChunkSize { get; init; } = 12000;

    public int ChunkOverlap { get; init; } = 200;
}