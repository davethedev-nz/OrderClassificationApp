using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OrderClassification.Application.Messaging;

namespace OrderClassification.Infrastructure.Messaging;

/// <summary>
/// In-memory event publisher for demonstration and testing.
/// In production, this would integrate with Azure Service Bus, RabbitMQ, or Kafka.
/// </summary>
public sealed class InMemoryEventPublisher : IIntegrationEventPublisher
{
    private static readonly List<(Type EventType, object Event, DateTime PublishedAt)> _publishedEvents = [];
    private readonly ILogger<InMemoryEventPublisher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public InMemoryEventPublisher(
        ILogger<InMemoryEventPublisher> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        lock (_publishedEvents)
        {
            _publishedEvents.Add((typeof(T), @event, DateTime.UtcNow));
        }

        _logger.LogInformation(
            "Event published: {EventType} at {PublishedAt}",
            typeof(T).Name,
            DateTime.UtcNow
        );

        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<T>>().ToList();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Dispatched event {EventType} to {HandlerCount} handler(s)",
            typeof(T).Name,
            handlers.Count
        );
    }

    /// <summary>
    /// For testing: retrieve all published events of a specific type.
    /// </summary>
    public static IReadOnlyList<T> GetPublishedEvents<T>() where T : class
    {
        lock (_publishedEvents)
        {
            return _publishedEvents
                .Where(e => e.EventType == typeof(T))
                .Select(e => (T)e.Event)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// For testing: clear all published events.
    /// </summary>
    public static void ClearPublishedEvents()
    {
        lock (_publishedEvents)
        {
            _publishedEvents.Clear();
        }
    }
}

