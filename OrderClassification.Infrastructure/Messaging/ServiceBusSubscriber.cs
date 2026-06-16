using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderClassification.Domain.Orders;

namespace OrderClassification.Infrastructure.Messaging;

/// <summary>
/// Background Service Bus subscriber that routes integration events to registered handlers.
/// This only starts when Messaging:Provider is ServiceBus and a connection string is configured.
/// </summary>
public sealed class ServiceBusSubscriber(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<ServiceBusSubscriber> logger)
    : BackgroundService
{
    private readonly MessagingOptions _options = options.Value;
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBusClient.CreateProcessor(
            _options.ServiceBus.TopicName,
            _options.ServiceBus.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = Math.Max(1, _options.ServiceBus.MaxConcurrentCalls)
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        logger.LogInformation(
            "Starting Service Bus subscriber for topic {TopicName} subscription {SubscriptionName}",
            _options.ServiceBus.TopicName,
            _options.ServiceBus.SubscriptionName);

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var eventType = args.Message.ApplicationProperties.TryGetValue("eventType", out var value)
                ? value?.ToString()
                : null;

            if (eventType == typeof(OrderClassificationIntegrationEvent).FullName ||
                args.Message.Subject == _options.ServiceBus.OrderClassifiedSubject)
            {
                var integrationEvent = JsonSerializer.Deserialize<OrderClassificationIntegrationEvent>(args.Message.Body.ToString());

                if (integrationEvent is null)
                {
                    throw new InvalidOperationException("Could not deserialize OrderClassificationIntegrationEvent from Service Bus message.");
                }

                using var scope = scopeFactory.CreateScope();
                var handlers = scope.ServiceProvider
                    .GetServices<IEventHandler<OrderClassificationIntegrationEvent>>()
                    .ToList();

                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(integrationEvent, args.CancellationToken);
                }

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            logger.LogWarning(
                "Dead-lettering unsupported Service Bus message. Subject={Subject}, EventType={EventType}",
                args.Message.Subject,
                eventType ?? "unknown");

            await args.DeadLetterMessageAsync(
                args.Message,
                "unsupported-event-type",
                "No handler mapping exists for the incoming message.",
                args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing Service Bus message {MessageId}", args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Service Bus processor error. EntityPath={EntityPath}, ErrorSource={ErrorSource}",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}

