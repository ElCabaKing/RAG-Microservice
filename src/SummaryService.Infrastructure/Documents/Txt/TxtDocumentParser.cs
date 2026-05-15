using System.Text;
using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Enums;
using SummaryService.Domain.ValueObjects;

namespace SummaryService.Infrastructure.Documents.Txt;

/// <summary>
/// Parser para archivos de texto (.txt).
/// Responsable de leer, validar y normalizar contenido de archivos de texto.
/// </summary>
public class TxtDocumentParser : IDocumentParser
{
    private readonly ILogger<TxtDocumentParser> _logger;
    private const int MaxBufferSize = 10 * 1024 * 1024; // 10 MB
    private const int MinContentLength = 1;

    public TxtDocumentParser(ILogger<TxtDocumentParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Procesa un archivo de texto de forma asincrónica.
    /// </summary>
    /// <param name="documentStream">Stream del archivo a procesar</param>
    /// <param name="fileName">Nombre del archivo</param>
    /// <param name="sizeInBytes">Tamaño del archivo en bytes</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Contenido del documento procesado</returns>
    /// <exception cref="ArgumentNullException">Si el stream es nulo</exception>
    /// <exception cref="InvalidOperationException">Si el archivo es muy grande o no contiene contenido válido</exception>
    public async Task<DocumentContent> ParseAsync(
        Stream documentStream,
        string fileName,
        long sizeInBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documentStream);

        _logger.LogInformation(
            "Iniciando parsing de archivo TXT: {FileName}, Tamaño: {SizeInBytes} bytes",
            fileName,
            sizeInBytes);

        // Validar tamaño del archivo
        if (sizeInBytes > MaxBufferSize)
        {
            _logger.LogError(
                "Archivo TXT excede el tamaño máximo permitido. Tamaño: {SizeInBytes} bytes, Máximo: {MaxBufferSize} bytes",
                sizeInBytes,
                MaxBufferSize);
            throw new InvalidOperationException(
                $"El archivo TXT excede el tamaño máximo permitido de {MaxBufferSize / 1024 / 1024} MB");
        }

        try
        {
            // Leer contenido del stream de forma UTF-8 segura
            string content = await ReadContentAsync(documentStream, cancellationToken);

            // Validar que hay contenido
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("El archivo TXT no contiene contenido válido: {FileName}", fileName);
                throw new InvalidOperationException("El archivo TXT está vacío o solo contiene espacios en blanco");
            }

            // Normalizar contenido
            string normalizedContent = NormalizeContent(content);

            if (normalizedContent.Length < MinContentLength)
            {
                _logger.LogWarning(
                    "El archivo TXT después de normalización contiene contenido insuficiente: {FileName}",
                    fileName);
                throw new InvalidOperationException("El contenido normalizado es insuficiente");
            }

            _logger.LogInformation(
                "Parsing de TXT completado exitosamente. Archivo: {FileName}, Contenido normalizado: {ContentLength} caracteres",
                fileName,
                normalizedContent.Length);

            return new DocumentContent(normalizedContent, DocumentType.Txt, sizeInBytes);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("El parsing del archivo TXT fue cancelado: {FileName}", fileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al procesar archivo TXT: {FileName}",
                fileName);
            throw;
        }
    }

    /// <summary>
    /// Lee el contenido del stream de forma asincrónica y UTF-8 segura.
    /// </summary>
    private static async Task<string> ReadContentAsync(Stream stream, CancellationToken cancellationToken)
    {
        // Resetear la posición del stream si es posible
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        // Usar StreamReader con UTF-8 y detección de BOM
        using var reader = new StreamReader(
            stream,
            encoding: Encoding.UTF8,
            bufferSize: 4096,
            leaveOpen: true);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Normaliza el contenido del documento.
    /// - Elimina espacios en blanco al inicio y final
    /// - Normaliza saltos de línea (CRLF → LF)
    /// - Elimina líneas vacías múltiples consecutivas
    /// </summary>
    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // Normalizar saltos de línea: CRLF y CR → LF
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Eliminar líneas vacías múltiples consecutivas
        var lines = content.Split('\n', StringSplitOptions.None);
        var normalizedLines = new List<string>();

        foreach (var line in lines)
        {
            // Si la línea no está vacía, agregarla
            // Si está vacía, solo agregarla si la anterior tampoco lo está (evita múltiples vacías)
            if (!string.IsNullOrWhiteSpace(line) ||
                (normalizedLines.Count > 0 && !string.IsNullOrWhiteSpace(normalizedLines[^1])))
            {
                normalizedLines.Add(line);
            }
        }

        // Unir líneas y eliminar espacios al inicio/final
        return string.Join("\n", normalizedLines).Trim();
    }
}
