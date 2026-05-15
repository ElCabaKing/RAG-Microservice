namespace SummaryService.Application.Interfaces;

public interface IPdfOcrExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken);
}