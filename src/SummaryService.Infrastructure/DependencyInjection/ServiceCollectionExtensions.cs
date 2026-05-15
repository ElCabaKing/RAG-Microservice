using Microsoft.Extensions.DependencyInjection;
using SummaryService.Application.Interfaces;
using SummaryService.Infrastructure.Documents.Normalization;
using SummaryService.Infrastructure.Documents.Parsers;
using SummaryService.Infrastructure.Documents.Pdf;
using SummaryService.Infrastructure.Documents.Strategies;
using SummaryService.Infrastructure.Documents.Txt;
using SummaryService.Infrastructure.Services;

namespace SummaryService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // PDF Rendering
        services.AddScoped<IPdfRenderer, PdfRenderer>();

        // OCR Extraction
        services.AddScoped<IPdfOcrExtractor, PdfOcrExtractor>();

        // Text Extraction
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

        // TXT Parser
        services.AddScoped<TxtDocumentParser>();

        // Document Processing Components
        services.AddScoped<TextNormalizer>();
        services.AddScoped<PdfOcrDetectionStrategy>();
        services.AddScoped<SmartPdfProcessor>();
        services.AddScoped<DocumentParserFactory>();
        services.AddScoped<DocumentProcessingService>();

        return services;
    }
}