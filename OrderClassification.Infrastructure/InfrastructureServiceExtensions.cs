using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderClassification.Domain.Orders;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure layer services.
///
/// Note: the in-memory repository is registered as Singleton so data
/// survives across requests during local dev. Day 3 replaces this
/// with a Scoped EF Core repository (one DbContext per request).
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Temporary: in-memory repository until Day 3 adds EF Core
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

        return services;
    }
}

