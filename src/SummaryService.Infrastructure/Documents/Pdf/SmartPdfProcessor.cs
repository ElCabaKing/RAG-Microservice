using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.ValueObjects;
using SummaryService.Infrastructure.Documents.Strategies;

namespace SummaryService.Infrastructure.Documents.Pdf;

/// <summary>
/// Procesador inteligente de PDFs que combina extracción textual y OCR.
/// Intenta extraer texto nativo primero, y aplica OCR como fallback si es necesario.
/// </summary>
public sealed class SmartPdfProcessor
{
    private readonly IPdfTextExtractor _textExtractor;
    private readonly IPdfOcrExtractor _ocrExtractor;
    private readonly PdfOcrDetectionStrategy _detectionStrategy;
    private readonly ILogger<SmartPdfProcessor> _logger;

    public SmartPdfProcessor(
        IPdfTextExtractor textExtractor,
        IPdfOcrExtractor ocrExtractor,
        PdfOcrDetectionStrategy detectionStrategy,
        ILogger<SmartPdfProcessor> logger)
    {
        _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
        _ocrExtractor = ocrExtractor ?? throw new ArgumentNullException(nameof(ocrExtractor));
        _detectionStrategy = detectionStrategy ?? throw new ArgumentNullException(nameof(detectionStrategy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Procesa un PDF aplicando la estrategia inteligente: intenta extracción de texto
    /// y aplica OCR automáticamente si es necesario.
    /// </summary>
    /// <param name="pdfStream">Stream del PDF a procesar</param>
    /// <param name="fileName">Nombre del archivo PDF</param>
    /// <param name="sizeInBytes">Tamaño del archivo en bytes</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DocumentContent con el texto extraído</returns>
    public async Task<DocumentContent> ProcessAsync(
        Stream pdfStream,
        string fileName,
        long sizeInBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        _logger.LogInformation(
            "Iniciando procesamiento inteligente de PDF: {FileName}, Tamaño: {SizeInBytes} bytes",
            fileName,
            sizeInBytes);

        try
        {
            // Paso 1: Intentar extracción textual
            _logger.LogInformation("Intentando extracción de texto nativo del PDF");
            string extractedText;

            try
            {
                extractedText = await _textExtractor.ExtractTextAsync(pdfStream, cancellationToken);
                _logger.LogInformation(
                    "Extracción de texto completada. Longitud: {TextLength} caracteres",
                    extractedText.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error durante extracción de texto nativo. Se recurrirá a OCR");
                extractedText = string.Empty;
            }

            // Paso 2: Decidir si aplicar OCR
            bool needsOcr = _detectionStrategy.ShouldApplyOcr(extractedText);

            if (needsOcr)
            {
                _logger.LogInformation("Detectado: Se requiere OCR. Iniciando pipeline OCR...");

                try
                {
                    // Resetear stream
                    if (pdfStream.CanSeek)
                    {
                        pdfStream.Position = 0;
                    }

                    var ocrText = await _ocrExtractor.ExtractTextAsync(pdfStream, cancellationToken);
                    extractedText = ocrText;

                    _logger.LogInformation(
                        "OCR completado. Longitud total: {TextLength} caracteres",
                        extractedText.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error durante extracción OCR. Se retornará el texto nativo disponible");
                    // Si OCR falla, usar el texto nativo aunque sea incompleto
                }
            }
            else
            {
                _logger.LogInformation("Detectado: Texto suficiente. OCR no es necesario.");
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("No se pudo extraer texto suficiente del PDF");
                throw new InvalidOperationException(
                    "No se pudo extraer texto del PDF ni mediante extracción nativa ni mediante OCR");
            }

            _logger.LogInformation(
                "Procesamiento de PDF completado. Texto final: {TextLength} caracteres",
                extractedText.Length);

            return new DocumentContent(
                Content: extractedText,
                Type: SummaryService.Domain.Enums.DocumentType.Pdf,
                SizeInBytes: sizeInBytes);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Procesamiento de PDF fue cancelado");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante procesamiento de PDF: {FileName}", fileName);
            throw;
        }
    }
}
