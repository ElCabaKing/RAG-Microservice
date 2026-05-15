using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Services;

/// <summary>
/// Renderiza páginas de PDF a imágenes usando Docnet.
/// Convertidas para procesamiento con OCR.
/// </summary>
public class PdfRenderer : IPdfRenderer
{
    private readonly ILogger<PdfRenderer> _logger;
    private const int DefaultDpi = 1920; // Width for rendering
    private const int DefaultHeight = 1080;

    public PdfRenderer(ILogger<PdfRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Renderiza todas las páginas de un PDF a imágenes.
    /// </summary>
    /// <param name="pdfStream">Stream del PDF a renderizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de arrays de bytes representando imágenes PNG</returns>
    /// <exception cref="InvalidOperationException">Si hay error renderizando</exception>
    public async Task<List<byte[]>> RenderPagesAsync(
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Iniciando renderización de PDF a imágenes");

        try
        {
            using var memory = new MemoryStream();

            await pdfStream.CopyToAsync(memory, cancellationToken);

            var bytes = memory.ToArray();

            _logger.LogInformation("PDF cargado en memoria. Tamaño: {SizeInBytes} bytes", bytes.Length);

            var images = new List<byte[]>();

            using var docReader = DocLib.Instance.GetDocReader(
                bytes,
                new PageDimensions(DefaultDpi, DefaultHeight));

            var pageCount = docReader.GetPageCount();
            _logger.LogInformation("Total de páginas a renderizar: {PageCount}", pageCount);

            for (int i = 0; i < pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Renderizando página {PageNumber}/{PageCount}", i + 1, pageCount);

                using var pageReader = docReader.GetPageReader(i);

                var imageBytes = pageReader.GetImage();
                images.Add(imageBytes);

                _logger.LogDebug(
                    "Página {PageNumber} renderizada. Tamaño imagen: {ImageSize} bytes",
                    i + 1,
                    imageBytes.Length);
            }

            _logger.LogInformation(
                "Renderización completada. Total imágenes: {ImageCount}",
                images.Count);

            return images;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Renderización de PDF fue cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renderizando PDF a imágenes");
            throw new InvalidOperationException("Error renderizando PDF", ex);
        }
    }
}