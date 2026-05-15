namespace SummaryService.Domain.Exceptions;

public sealed class UnsupportedDocumentException : Exception
{
    public UnsupportedDocumentException(string message)
        : base(message)
    {
    }
}