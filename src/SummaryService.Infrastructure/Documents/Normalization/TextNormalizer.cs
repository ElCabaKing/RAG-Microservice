using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Documents.Normalization;

/// <summary>
/// Normaliza contenido textual extraído de documentos.
/// Limpia artefactos de OCR, espacios excesivos y caracteres inválidos
/// SIN destruir la semántica del contenido.
/// </summary>
public sealed class TextNormalizer
{
    private readonly ILogger<TextNormalizer> _logger;

    public TextNormalizer(ILogger<TextNormalizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Normaliza el texto eliminando artefactos y espacios excesivos.
    /// </summary>
    /// <param name="text">Texto a normalizar</param>
    /// <returns>Texto normalizado</returns>
    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        _logger.LogDebug("Iniciando normalización de texto. Longitud original: {OriginalLength}", text.Length);

        try
        {
            // 1. Reemplazar múltiples espacios en blanco con uno solo
            var step1 = Regex.Replace(text, @"\s+", " ");

            // 2. Normalizar saltos de línea: múltiples newlines → máximo 2
            var step2 = Regex.Replace(step1, @"\n\s*\n[\s\n]*", "\n\n");

            // 3. Remover espacios antes de puntuación
            var step3 = Regex.Replace(step2, @"\s+([.,;:!?\)])", "$1");

            // 4. Limpiar artefactos comunes de OCR
            var step4 = CleanOcrArtifacts(step3);

            // 5. Normalizar espacios al inicio y final de líneas
            var lines = step4.Split('\n');
            var normalizedLines = lines.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line));
            var step5 = string.Join("\n", normalizedLines);

            // 6. Asegurar al menos un espacio después de puntuación (excepto al final de línea)
            var step6 = Regex.Replace(step5, @"([.!?])([A-Z])", "$1 $2");

            var result = step6.Trim();

            _logger.LogDebug(
                "Normalización completada. Longitud original: {OriginalLength}, Normalizada: {NormalizedLength}",
                text.Length,
                result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la normalización del texto");
            throw;
        }
    }

    /// <summary>
    /// Limpia artefactos comunes de OCR.
    /// </summary>
    private static string CleanOcrArtifacts(string text)
    {
        var result = text;

        // Reemplazar caracteres similares que OCR confunde frecuentemente
        var ocrReplacements = new Dictionary<string, string>
        {
            // Letras comúnmente confundidas por OCR
            { "rn", "m" }, // En contexto específico (opcional, comentado por seguridad semántica)
            // Números y letras
            { "0", "O" }, // En contextos donde probablemente sea letra
            { "1", "l" }, // En contextos donde probablemente sea letra
            { "5", "S" }, // En contextos donde probablemente sea letra
        };

        // NO aplicamos reemplazos directos agresivos para preservar semántica
        // En su lugar, solo limpiamos caracteres claramente inválidos
        result = Regex.Replace(result, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", ""); // Control chars
        result = Regex.Replace(result, @"[\x7F-\x9F]", ""); // Delete + C1 control codes

        return result;
    }
}
