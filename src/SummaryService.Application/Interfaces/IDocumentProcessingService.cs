using SummaryService.Domain.ValueObjects;

namespace SummaryService.Application.Interfaces;

public interface IDocumentProcessingService
{
    Task<DocumentContent> ProcessDocumentAsync(
        Stream documentStream,
        string fileName,
        string? contentType,
        long sizeInBytes,
        CancellationToken cancellationToken);
}
