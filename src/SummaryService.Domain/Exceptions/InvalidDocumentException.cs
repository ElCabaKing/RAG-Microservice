namespace SummaryService.Domain.Exceptions;

public sealed class InvalidDocumentException : Exception
{
    public InvalidDocumentException(string message)
        : base(message)
    {
    }
}