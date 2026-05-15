namespace SummaryService.Api.Configurations;

public sealed class SummaryOptions
{
    public const string SectionName = "Summary";

    public int MaxFileSizeMb { get; init; } = 15;

    public int ChunkSize { get; init; } = 12000;

    public int MaxTokens { get; init; } = 2048;
}