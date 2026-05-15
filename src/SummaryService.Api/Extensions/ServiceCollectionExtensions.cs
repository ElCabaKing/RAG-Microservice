using SummaryService.Infrastructure.DependencyInjection;

namespace SummaryService.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRagServices(this IServiceCollection services)
    {
        services.AddInfrastructureServices();

        return services;
    }
}