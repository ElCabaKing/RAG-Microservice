namespace SummaryService.Domain.Exceptions;

public sealed class SummaryGenerationException : Exception
{
    public SummaryGenerationException(string message)
        : base(message)
    {
    }
}