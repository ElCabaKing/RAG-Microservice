namespace SummaryService.Domain.Constants;

public static class SseEvents
{
    public const string Status = "status";
    public const string Chunk = "chunk";
    public const string Completed = "completed";
    public const string Error = "error";
}