using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Exceptions;
using SummaryService.Domain.ValueObjects;
using SummaryService.Infrastructure.Documents.Normalization;
using SummaryService.Infrastructure.Documents.Parsers;
using SummaryService.Shared.Helpers;

namespace SummaryService.Infrastructure.Services;

/// <summary>
/// Servicio orquestador principal para procesamiento documental.
/// Coordina validación, parsing, normalización y retorno de contenido listo para chunking.
/// </summary>
public sealed class DocumentProcessingService : IDocumentProcessingService
{
    private readonly DocumentParserFactory _parserFactory;
    private readonly TextNormalizer _textNormalizer;
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly int _maxFileSizeBytes;

    public DocumentProcessingService(
        DocumentParserFactory parserFactory,
        TextNormalizer textNormalizer,
        ILogger<DocumentProcessingService> logger,
        int maxFileSizeBytes = 15 * 1024 * 1024) // 15 MB default
    {
        _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
        _textNormalizer = textNormalizer ?? throw new ArgumentNullException(nameof(textNormalizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxFileSizeBytes = maxFileSizeBytes;
    }

    /// <summary>
    /// Procesa un documento completo: validación → parsing → normalización → retorno.
    /// </summary>
    /// <param name="documentStream">Stream del documento a procesar</param>
    /// <param name="fileName">Nombre del archivo</param>
    /// <param name="contentType">MIME type del archivo</param>
    /// <param name="sizeInBytes">Tamaño del archivo en bytes</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DocumentContent normalizado listo para chunking</returns>
    /// <exception cref="InvalidDocumentException">Si el documento es inválido</exception>
    /// <exception cref="UnsupportedDocumentException">Si el tipo de documento no es soportado</exception>
    public async Task<DocumentContent> ProcessDocumentAsync(
        Stream documentStream,
        string fileName,
        string? contentType,
        long sizeInBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documentStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        _logger.LogInformation(
            "Iniciando procesamiento de documento: {FileName}, Tamaño: {SizeInBytes} bytes, ContentType: {ContentType}",
            fileName,
            sizeInBytes,
            contentType ?? "desconocido");

        try
        {
            // Paso 1: Validar documento
            ValidateDocument(fileName, contentType, sizeInBytes);

            // Paso 2: Resolver parser
            var parser = _parserFactory.ResolveParser(fileName);
            _logger.LogInformation("Parser resuelto para {FileName}", fileName);

            // Paso 3: Extraer contenido
            _logger.LogInformation("Iniciando parsing del documento");
            var rawContent = await parser.ParseAsync(
                documentStream,
                fileName,
                sizeInBytes,
                cancellationToken);

            _logger.LogInformation(
                "Parsing completado. Contenido extraído: {ContentLength} caracteres",
                rawContent.Content.Length);

            // Paso 4: Normalizar texto
            _logger.LogInformation("Normalizando contenido del documento");
            var normalizedText = _textNormalizer.Normalize(rawContent.Content);

            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                _logger.LogError(
                    "El documento no contiene contenido válido después de normalización: {FileName}",
                    fileName);
                throw new InvalidDocumentException(
                    "El documento no contiene contenido válido para procesar");
            }

            // Paso 5: Retornar contenido normalizado
            var result = new DocumentContent(
                Content: normalizedText,
                Type: rawContent.Type,
                SizeInBytes: sizeInBytes);

            _logger.LogInformation(
                "Procesamiento de documento completado. Contenido final: {ContentLength} caracteres",
                result.Content.Length);

            return result;
        }
        catch (InvalidDocumentException)
        {
            _logger.LogError("Documento inválido: {FileName}", fileName);
            throw;
        }
        catch (UnsupportedDocumentException)
        {
            _logger.LogError("Tipo de documento no soportado: {FileName}", fileName);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Procesamiento de documento fue cancelado: {FileName}", fileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando documento: {FileName}", fileName);
            throw new InvalidDocumentException(
                $"Error procesando documento: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida el documento antes del procesamiento.
    /// </summary>
    /// <exception cref="InvalidDocumentException">Si el documento no cumple validaciones</exception>
    /// <exception cref="UnsupportedDocumentException">Si el tipo no es soportado</exception>
    private void ValidateDocument(string fileName, string? contentType, long sizeInBytes)
    {
        _logger.LogInformation("Validando documento: {FileName}", fileName);

        // Validación 1: Tamaño
        if (sizeInBytes <= 0)
        {
            _logger.LogError("Documento vacío: {FileName}", fileName);
            throw new InvalidDocumentException("El documento está vacío");
        }

        if (sizeInBytes > _maxFileSizeBytes)
        {
            _logger.LogError(
                "Documento excede tamaño máximo. Tamaño: {SizeInBytes}, Máximo: {MaxSize}",
                sizeInBytes,
                _maxFileSizeBytes);
            throw new InvalidDocumentException(
                $"El documento excede el tamaño máximo permitido de {_maxFileSizeBytes / 1024 / 1024} MB");
        }

        // Validación 2: MIME type
        if (!string.IsNullOrEmpty(contentType) && !FileHelper.IsSupportedMimeType(contentType))
        {
            _logger.LogError("MIME type no soportado: {ContentType}", contentType);
            throw new UnsupportedDocumentException(
                $"El tipo de archivo no es soportado: {contentType}");
        }

        // Validación 3: Extensión
        var documentType = FileHelper.GetDocumentTypeFromExtension(fileName);
        if (!documentType.HasValue)
        {
            _logger.LogError("Extensión de archivo no soportada: {FileName}", fileName);
            throw new UnsupportedDocumentException(
                $"La extensión del archivo no es soportada: {Path.GetExtension(fileName)}");
        }

        _logger.LogInformation("Documento validado correctamente: {FileName}", fileName);
    }
}
