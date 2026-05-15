using System.Threading;

namespace SummaryService.Application.Interfaces;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(System.IO.Stream pdfStream, CancellationToken cancellationToken);
}
