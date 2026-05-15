namespace SummaryService.Application.Interfaces;

public interface IStreamingTextGenerator
{
    IAsyncEnumerable<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
}