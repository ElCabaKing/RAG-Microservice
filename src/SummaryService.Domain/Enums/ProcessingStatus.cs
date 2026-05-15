namespace SummaryService.Domain.Enums;

public enum ProcessingStatus
{
    ExtractingText = 1,
    RunningOcr = 2,
    ChunkingDocument = 3,
    GeneratingSummary = 4,
    ReducingSummary = 5,
    Completed = 6,
    Failed = 7
}
