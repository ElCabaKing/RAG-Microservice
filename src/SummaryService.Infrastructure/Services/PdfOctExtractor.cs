using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;
using Tesseract;

namespace SummaryService.Infrastructure.Services;

/// <summary>
/// Extractor de texto mediante OCR usando Tesseract.
/// Procesa imágenes de páginas PDF y retorna texto reconocido.
/// </summary>
public sealed class PdfOcrExtractor : IPdfOcrExtractor, IAsyncDisposable
{
    private readonly IPdfRenderer _pdfRenderer;
    private readonly ILogger<PdfOcrExtractor> _logger;
    private TesseractEngine? _tesseractEngine;
    private readonly int _maxPages;
    private readonly int _timeoutSeconds;
    private bool _disposed;

    public PdfOcrExtractor(
        IPdfRenderer pdfRenderer,
        ILogger<PdfOcrExtractor> logger,
        int maxPages = 100,
        int timeoutSeconds = 60)
    {
        _pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxPages = maxPages;
        _timeoutSeconds = timeoutSeconds;

        InitializeTesseract();
    }

    /// <summary>
    /// Inicializa el motor de Tesseract.
    /// </summary>
    private void InitializeTesseract()
    {
        try
        {
            _logger.LogDebug("Inicializando Tesseract OCR Engine con idiomas: eng, spa");
            _tesseractEngine = new TesseractEngine(@"./tessdata", "eng+spa", EngineMode.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar Tesseract Engine");
            throw;
        }
    }

    /// <summary>
    /// Extrae texto de un PDF mediante OCR procesando página por página.
    /// </summary>
    /// <param name="pdfStream">Stream del PDF a procesar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Texto extraído mediante OCR</returns>
    /// <exception cref="OperationCanceledException">Si se cancela la operación</exception>
    /// <exception cref="InvalidOperationException">Si el PDF excede el límite de páginas</exception>
    public async Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Iniciando extracción OCR del PDF");

        var startTime = DateTime.UtcNow;

        try
        {
            // 1. Renderizar PDF a imágenes
            _logger.LogInformation("Renderizando PDF a imágenes...");
            var pageImages = await _pdfRenderer.RenderPagesAsync(pdfStream, cancellationToken);

            if (pageImages.Count > _maxPages)
            {
                _logger.LogError(
                    "El PDF excede el límite de páginas permitidas ({PageCount} > {MaxPages})",
                    pageImages.Count,
                    _maxPages);
                throw new InvalidOperationException(
                    $"El PDF tiene {pageImages.Count} páginas, máximo permitido: {_maxPages}");
            }

            _logger.LogInformation("PDF renderizado: {PageCount} páginas", pageImages.Count);

            // 2. Procesar cada página con OCR
            var ocrTexts = new ConcurrentBag<(int PageIndex, string Text)>();
            
            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 2 // Limitar paralelismo para controlar memoria
            };

            await Task.Run(
                () => Parallel.ForEach(pageImages.Select((bytes, idx) => (idx, bytes)), options, (item, loopState) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var (pageIndex, imageBytes) = item;

                    _logger.LogDebug("Procesando página {PageNumber} con OCR...", pageIndex + 1);

                    try
                    {
                        var pageText = ProcessPageWithOcr(imageBytes, pageIndex);
                        ocrTexts.Add((pageIndex, pageText));

                        _logger.LogDebug(
                            "Página {PageNumber} procesada. Texto extraído: {TextLength} caracteres",
                            pageIndex + 1,
                            pageText.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Error procesando página {PageNumber} con OCR",
                            pageIndex + 1);
                        ocrTexts.Add((pageIndex, string.Empty));
                    }
                }),
                cancellationToken);

            // 3. Combinar textos en orden
            var combinedText = string.Join(
                "\n\n",
                ocrTexts
                    .OrderBy(x => x.PageIndex)
                    .Select(x => x.Text)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            var elapsedTime = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Extracción OCR completada. Total: {PageCount} páginas, {TotalCharacters} caracteres, Tiempo: {ElapsedMs}ms",
                pageImages.Count,
                combinedText.Length,
                elapsedTime.TotalMilliseconds);

            return combinedText;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Extracción OCR fue cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la extracción OCR");
            throw;
        }
    }

    /// <summary>
    /// Procesa una página individual con OCR.
    /// </summary>
    private string ProcessPageWithOcr(byte[] imageBytes, int pageIndex)
    {
        if (_tesseractEngine == null)
        {
            throw new InvalidOperationException("Tesseract Engine no está inicializado");
        }

        try
        {
            using var image = Pix.LoadFromMemory(imageBytes);
            using var page = _tesseractEngine.Process(image);
            
            var text = page.GetText();
            
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Página {PageNumber} no contiene texto reconocible", pageIndex + 1);
                return string.Empty;
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando página {PageNumber}", pageIndex + 1);
            throw;
        }
    }

    /// <summary>
    /// Libera recursos de Tesseract.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Liberando recursos de Tesseract OCR");
        _tesseractEngine?.Dispose();
        _disposed = true;
        await Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PdfOcrExtractor));
        }
    }
}