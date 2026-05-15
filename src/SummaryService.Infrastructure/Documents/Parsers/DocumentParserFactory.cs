using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Enums;
using SummaryService.Infrastructure.Documents.Pdf;
using SummaryService.Infrastructure.Documents.Txt;
using SummaryService.Shared.Helpers;

namespace SummaryService.Infrastructure.Documents.Parsers;

/// <summary>
/// Fábrica que resuelve el parser correcto según el tipo de documento.
/// Soporta PDFs y archivos TXT de forma extensible.
/// </summary>
public sealed class DocumentParserFactory
{
    private readonly SmartPdfProcessor _pdfProcessor;
    private readonly TxtDocumentParser _txtParser;
    private readonly ILogger<DocumentParserFactory> _logger;

    public DocumentParserFactory(
        SmartPdfProcessor pdfProcessor,
        TxtDocumentParser txtParser,
        ILogger<DocumentParserFactory> logger)
    {
        _pdfProcessor = pdfProcessor ?? throw new ArgumentNullException(nameof(pdfProcessor));
        _txtParser = txtParser ?? throw new ArgumentNullException(nameof(txtParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene el parser apropiado basado en el tipo de documento.
    /// </summary>
    /// <param name="fileName">Nombre del archivo</param>
    /// <returns>Parser apropiado para el archivo</returns>
    /// <exception cref="InvalidOperationException">Si el tipo de documento no es soportado</exception>
    public IDocumentParser ResolveParser(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var documentType = FileHelper.GetDocumentTypeFromExtension(fileName);

        if (!documentType.HasValue)
        {
            _logger.LogError("Tipo de documento no soportado: {FileName}", fileName);
            throw new InvalidOperationException(
                $"El tipo de documento no es soportado: {fileName}");
        }

        _logger.LogInformation(
            "Parser resuelto para {FileName}: {DocumentType}",
            fileName,
            documentType.Value);

        return documentType.Value switch
        {
            DocumentType.Pdf => new SmartPdfProcessorAdapter(_pdfProcessor, _logger),
            DocumentType.Txt => _txtParser,
            _ => throw new InvalidOperationException(
                $"Tipo de documento no manejado: {documentType.Value}")
        };
    }

    /// <summary>
    /// Adaptador que expone SmartPdfProcessor como IDocumentParser.
    /// </summary>
    private sealed class SmartPdfProcessorAdapter : IDocumentParser
    {
        private readonly SmartPdfProcessor _processor;
        private readonly ILogger _logger;

        public SmartPdfProcessorAdapter(SmartPdfProcessor processor, ILogger logger)
        {
            _processor = processor;
            _logger = logger;
        }

        public Task<Domain.ValueObjects.DocumentContent> ParseAsync(
            Stream documentStream,
            string fileName,
            long sizeInBytes,
            CancellationToken cancellationToken)
        {
            return _processor.ProcessAsync(documentStream, fileName, sizeInBytes, cancellationToken);
        }
    }
}
