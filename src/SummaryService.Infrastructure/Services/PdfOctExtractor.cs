using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Services;

/// <summary>
/// Extractor de texto mediante OCR usando Tesseract CLI.
/// Procesa imágenes de páginas PDF y retorna texto reconocido.
/// </summary>
public sealed class PdfOcrExtractor : IPdfOcrExtractor, IAsyncDisposable
{
    private readonly IPdfRenderer _pdfRenderer;
    private readonly ILogger<PdfOcrExtractor> _logger;
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

            var pageImages = await _pdfRenderer.RenderPagesAsync(
                pdfStream,
                cancellationToken);

            if (pageImages.Count > _maxPages)
            {
                _logger.LogError(
                    "El PDF excede el límite de páginas permitidas ({PageCount} > {MaxPages})",
                    pageImages.Count,
                    _maxPages);

                throw new InvalidOperationException(
                    $"El PDF tiene {pageImages.Count} páginas, máximo permitido: {_maxPages}");
            }

            _logger.LogInformation(
                "PDF renderizado: {PageCount} páginas",
                pageImages.Count);

            // 2. Procesar páginas con OCR
            var ocrTexts = new ConcurrentBag<(int PageIndex, string Text)>();

            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 2
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(
                    pageImages.Select((bytes, idx) => (idx, bytes)),
                    options,
                    item =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var (pageIndex, imageBytes) = item;

                        _logger.LogDebug(
                            "Procesando página {PageNumber} con OCR...",
                            pageIndex + 1);

                        try
                        {
                            var pageText = ProcessPageWithOcrAsync(
                                    imageBytes,
                                    pageIndex,
                                    cancellationToken)
                                .GetAwaiter()
                                .GetResult();

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
                    });
            }, cancellationToken);

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
    /// Procesa una página individual usando Tesseract CLI.
    /// </summary>
    private async Task<string> ProcessPageWithOcrAsync(
        byte[] imageBytes,
        int pageIndex,
        CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "summary-service-ocr");

        Directory.CreateDirectory(tempDirectory);

        var imagePath = Path.Combine(
            tempDirectory,
            $"ocr-page-{Guid.NewGuid()}.png");

        try
        {
            await File.WriteAllBytesAsync(
                imagePath,
                imageBytes,
                cancellationToken);

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{imagePath}\" stdout -l eng+spa",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogDebug(
                "Ejecutando OCR para página {PageNumber}",
                pageIndex + 1);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

            await process.WaitForExitAsync(timeoutCts.Token);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Tesseract OCR falló para página {pageIndex + 1}: {error}");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning(
                    "Página {PageNumber} no contiene texto reconocible",
                    pageIndex + 1);

                return string.Empty;
            }

            return output.Trim();
        }
        finally
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo eliminar archivo temporal OCR: {ImagePath}",
                    imagePath);
            }
        }
    }

    /// <summary>
    /// Libera recursos.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PdfOcrExtractor));
        }
    }
}