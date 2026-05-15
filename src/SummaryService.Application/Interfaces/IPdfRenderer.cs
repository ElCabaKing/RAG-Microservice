
namespace SummaryService.Application.Interfaces;
public interface IPdfRenderer
{
    Task<List<byte[]>> RenderPagesAsync(
        Stream pdfStream,
        CancellationToken cancellationToken);
}