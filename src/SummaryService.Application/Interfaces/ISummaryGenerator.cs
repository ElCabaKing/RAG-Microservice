using System.Threading;
using SummaryService.Domain.Enums;

namespace SummaryService.Application.Interfaces;

public interface ISummaryGenerator
{
    IAsyncEnumerable<string> GenerateSummaryAsync(System.IO.Stream documentStream, SummaryStyle style, int maxTokens, CancellationToken cancellationToken);
}
