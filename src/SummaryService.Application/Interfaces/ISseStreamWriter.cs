namespace SummaryService.Application.Interfaces;

public interface ISseStreamWriter
{
    Task WriteEventAsync(Stream outputStream, string eventName, object data, CancellationToken cancellationToken);
}