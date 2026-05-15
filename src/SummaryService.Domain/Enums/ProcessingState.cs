namespace SummaryService.Domain.Enums;

public enum ProcessingState
{
    ExtractingText,
    RunningOcr,
    ChunkingDocument,
    GeneratingSummary,
    ReducingSummary,
    Completed
}
