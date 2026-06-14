using Microsoft.Extensions.DependencyInjection;
using OrderClassification.Application.Orders.Commands;
using OrderClassification.Application.Orders.Queries;
using OrderClassification.Application.Services;

namespace OrderClassification.Application;

/// <summary>
/// Extension method to register all Application layer services.
///
/// This pattern keeps Program.cs thin.
/// Each layer owns its own DI registration — same principle as
/// having separate factory/provider setup in Python.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<ClassifyOrderHandler>();
        services.AddScoped<DomainEventPublisher>();

        return services;
    }
}

