namespace SummaryService.Domain.Exceptions;

public sealed class OcrProcessingException : Exception
{
    public OcrProcessingException(string message)
        : base(message)
    {
    }
}