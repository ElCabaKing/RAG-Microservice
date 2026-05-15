using SummaryService.Application.Interfaces;
using SummaryService.Domain.Enums;
using System.Threading;

namespace SummaryService.Infrastructure.Services;

public class PlaceholderSummaryGenerator : ISummaryGenerator
{
    public async IAsyncEnumerable<string> GenerateSummaryAsync(System.IO.Stream documentStream, SummaryStyle style, int maxTokens, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simple placeholder that yields a few chunks with delays to simulate streaming
        var chunks = new[] { "summary chunk 1", "summary chunk 2", "summary chunk 3" };
        foreach (var c in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(200, cancellationToken);
            yield return c;
        }
    }
}
