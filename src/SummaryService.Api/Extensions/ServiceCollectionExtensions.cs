using SummaryService.Application.Interfaces;
using SummaryService.Infrastructure.DependencyInjection;
using SummaryService.Infrastructure.Services;

namespace SummaryService.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRagServices(this IServiceCollection services)
    {
        services.AddInfrastructureServices();
        services.AddSingleton<ISummaryGenerator, PlaceholderSummaryGenerator>();
        services.AddSingleton<IPdfTextExtractor, PlaceholderPdfTextExtractor>();

        return services;
    }
}