using Microsoft.Extensions.Logging;

namespace SummaryService.Infrastructure.Documents.Strategies;

/// <summary>
/// Determina si un PDF requiere OCR basado en la cantidad y calidad de texto extraído.
/// </summary>
public sealed class PdfOcrDetectionStrategy
{
    private readonly ILogger<PdfOcrDetectionStrategy> _logger;
    private readonly int _minTextLength;

    public PdfOcrDetectionStrategy(
        ILogger<PdfOcrDetectionStrategy> logger,
        int minTextLength = 500)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _minTextLength = minTextLength;
    }

    /// <summary>
    /// Determina si el texto extraído es suficiente o si se necesita OCR.
    /// </summary>
    /// <param name="extractedText">Texto extraído del PDF</param>
    /// <returns>True si se necesita OCR, False si el texto es suficiente</returns>
    public bool ShouldApplyOcr(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            _logger.LogInformation("Texto vacío detectado. Se aplicará OCR.");
            return true;
        }

        // Criterio 1: Longitud mínima
        if (extractedText.Length < _minTextLength)
        {
            _logger.LogInformation(
                "Texto insuficiente detectado (Longitud: {TextLength}, Mínimo: {MinLength}). Se aplicará OCR.",
                extractedText.Length,
                _minTextLength);
            return true;
        }

        // Criterio 2: Proporción de espacios en blanco excesivos
        var whitespaceRatio = CountWhitespace(extractedText) / (double)extractedText.Length;
        if (whitespaceRatio > 0.5)
        {
            _logger.LogInformation(
                "Proporción de espacios en blanco excesiva ({Ratio:P}). Se aplicará OCR.",
                whitespaceRatio);
            return true;
        }

        // Criterio 3: Caracteres inválidos o corruptos
        var invalidCharRatio = CountInvalidCharacters(extractedText) / (double)extractedText.Length;
        if (invalidCharRatio > 0.1)
        {
            _logger.LogInformation(
                "Alta proporción de caracteres inválidos ({Ratio:P}). Se aplicará OCR.",
                invalidCharRatio);
            return true;
        }

        _logger.LogInformation("Texto suficiente detectado. OCR no es necesario.");
        return false;
    }

    /// <summary>
    /// Cuenta caracteres de espacio en blanco.
    /// </summary>
    private static int CountWhitespace(string text)
    {
        return text.Count(char.IsWhiteSpace);
    }

    /// <summary>
    /// Cuenta caracteres que se consideran inválidos o corruptos.
    /// </summary>
    private static int CountInvalidCharacters(string text)
    {
        return text.Count(c =>
            char.IsControl(c) && c != '\n' && c != '\r' && c != '\t' ||
            (c >= '\x7F' && c <= '\x9F'));
    }
}
