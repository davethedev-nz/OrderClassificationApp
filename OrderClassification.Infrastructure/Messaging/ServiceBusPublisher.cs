using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderClassification.Application.Messaging;
using OrderClassification.Domain.Orders;

namespace OrderClassification.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events to Azure Service Bus.
/// For this cram, the OrderClassificationIntegrationEvent maps to a topic message.
/// </summary>
public sealed class ServiceBusPublisher(
    ServiceBusClient serviceBusClient,
    IOptions<MessagingOptions> options,
    ILogger<ServiceBusPublisher> logger)
    : IIntegrationEventPublisher, IAsyncDisposable
{
    private readonly MessagingOptions _options = options.Value;
    private ServiceBusSender? _sender;

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        _sender ??= serviceBusClient.CreateSender(_options.ServiceBus.TopicName);

        var message = CreateMessage(@event);
        await _sender.SendMessageAsync(message, cancellationToken);

        logger.LogInformation(
            "Published {EventType} to Service Bus topic {TopicName}",
            typeof(T).Name,
            _options.ServiceBus.TopicName);
    }

    private ServiceBusMessage CreateMessage<T>(T @event) where T : class
    {
        var payload = JsonSerializer.Serialize(@event);
        var message = new ServiceBusMessage(payload)
        {
            ContentType = "application/json",
            Subject = ResolveSubject(@event),
            MessageId = ResolveMessageId(@event),
            CorrelationId = ResolveCorrelationId(@event)
        };

        message.ApplicationProperties["eventType"] = typeof(T).FullName ?? typeof(T).Name;

        return message;
    }

    private string ResolveSubject<T>(T @event) where T : class => @event switch
    {
        OrderClassificationIntegrationEvent => _options.ServiceBus.OrderClassifiedSubject,
        _ => typeof(T).Name
    };

    private static string ResolveMessageId<T>(T @event) where T : class => @event switch
    {
        OrderClassificationIntegrationEvent integrationEvent => integrationEvent.EventId.ToString("N"),
        _ => Guid.NewGuid().ToString("N")
    };

    private static string? ResolveCorrelationId<T>(T @event) where T : class => @event switch
    {
        OrderClassificationIntegrationEvent integrationEvent => integrationEvent.CorrelationId,
        _ => null
    };

    public async ValueTask DisposeAsync()
    {
        if (_sender is not null)
        {
            await _sender.DisposeAsync();
        }
    }
}

