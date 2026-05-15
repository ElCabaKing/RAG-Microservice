namespace SummaryService.Domain.Enums;

public enum StreamEventType
{
    Status = 1,
    Chunk = 2,
    Completed = 3,
    Error = 4
}