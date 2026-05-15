using SummaryService.Domain.ValueObjects;

namespace SummaryService.Application.Interfaces;

public interface IDocumentParser
{
    Task<DocumentContent> ParseAsync(Stream documentStream, string fileName, long sizeInBytes, CancellationToken cancellationToken);
}