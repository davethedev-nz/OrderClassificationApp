using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderClassification.Application.Messaging;
using OrderClassification.Domain.Orders;
using OrderClassification.Infrastructure.Messaging;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Infrastructure;

/// <summary>
/// Infrastructure wiring for persistence and messaging.
///
/// Python analogy: this is where we bind the SQLAlchemy engine/session
/// to the repository implementation, and wire up Celery/RabbitMQ publishers.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
        var connectionString = configuration.GetConnectionString("OrderClassification")
            ?? "Data Source=orderclassification.db";

        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
        services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        
        // Only register outbox dispatcher if enabled in configuration
        if (messagingOptions.Outbox.Enabled)
        {
            services.AddHostedService<OutboxDispatcher>();
        }
        
        services.AddScoped<IEventHandler<OrderClassificationIntegrationEvent>, OrderClassificationEventHandler>();

        // Event publishing and handling
        if (messagingOptions.UseServiceBus() && !string.IsNullOrWhiteSpace(messagingOptions.ServiceBus.ConnectionString))
        {
            services.AddSingleton(_ => new ServiceBusClient(messagingOptions.ServiceBus.ConnectionString));
            services.AddSingleton<IIntegrationEventPublisher, ServiceBusPublisher>();
            services.AddHostedService<ServiceBusSubscriber>();
        }
        else
        {
            services.AddSingleton<IIntegrationEventPublisher, InMemoryEventPublisher>();
        }

        return services;
    }
}

