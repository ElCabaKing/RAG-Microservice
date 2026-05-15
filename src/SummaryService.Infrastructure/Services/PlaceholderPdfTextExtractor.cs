using SummaryService.Application.Interfaces;
using System.Threading;

namespace SummaryService.Infrastructure.Services;

public class PlaceholderPdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(System.IO.Stream pdfStream, CancellationToken cancellationToken)
    {
        // Placeholder: in real implementation use PdfPig/Tesseract
        return Task.FromResult("[extracted text placeholder]");
    }
}
