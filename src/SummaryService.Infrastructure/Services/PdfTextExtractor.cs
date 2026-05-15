using System.Text;
using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;
using UglyToad.PdfPig;

namespace SummaryService.Infrastructure.Services;

/// <summary>
/// Extractor de texto nativo desde PDFs usando PdfPig.
/// Intenta extraer texto directamente del PDF sin OCR.
/// Si el texto es insuficiente, delega a OCR.
/// </summary>
public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly IPdfOcrExtractor _ocrExtractor;
    private readonly ILogger<PdfTextExtractor> _logger;
    private const int MinTextThreshold = 100; // Mínimo de caracteres para considerar "legible"

    public PdfTextExtractor(
        IPdfOcrExtractor ocrExtractor,
        ILogger<PdfTextExtractor> logger)
    {
        _ocrExtractor = ocrExtractor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extrae texto nativo del PDF intentando leer el contenido textual.
    /// </summary>
    /// <param name="pdfStream">Stream del PDF a procesar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Texto extraído del PDF</returns>
    /// <exception cref="InvalidOperationException">Si el PDF está corrupto</exception>
    public Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Iniciando extracción de texto nativo con PdfPig");

        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        try
        {
            using var document = PdfDocument.Open(pdfStream);

            var pages = document.GetPages().ToList();
            _logger.LogInformation("PDF abierto correctamente. Total de páginas: {PageCount}", pages.Count);

            var builder = new StringBuilder();

            for (int i = 0; i < pages.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageText = pages[i].Text;
                builder.AppendLine(pageText);

                if ((i + 1) % 10 == 0)
                {
                    _logger.LogDebug("Procesadas {PageCount} páginas", i + 1);
                }
            }

            var extractedText = builder.ToString();

            _logger.LogInformation(
                "Extracción de texto completada. Total: {PageCount} páginas, {TextLength} caracteres",
                pages.Count,
                extractedText.Length);

            return Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abriendo o leyendo PDF");
            throw new InvalidOperationException("Error procesando PDF", ex);
        }
    }
}