namespace SummaryService.Domain.Exceptions;

public sealed class ChunkingException : Exception
{
    public ChunkingException(string message)
        : base(message)
    {
    }
}