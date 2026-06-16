using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderClassification.Application.Messaging;
using OrderClassification.Domain.Orders;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Infrastructure.Messaging;

/// <summary>
/// Minimal outbox dispatcher: reads pending outbox rows and publishes them using the configured transport.
/// </summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<OutboxDispatcher> logger)
    : BackgroundService, IOutboxDispatcher
{
    private readonly MessagingOptions _options = options.Value;

    public Task DispatchAsync(CancellationToken cancellationToken = default) => RunOnceAsync(cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Outbox.Enabled)
        {
            logger.LogInformation("Outbox dispatcher is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(50, _options.Outbox.DispatchIntervalMilliseconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var batch = await dbContext.OutboxMessages
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.OccurredAt)
            .Take(Math.Max(1, _options.Outbox.BatchSize))
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            try
            {
                await PublishOutboxMessageAsync(publisher, message, cancellationToken);
                message.Status = "Dispatched";
                message.DispatchedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Status = "Failed";
                message.Error = ex.Message;
                logger.LogError(ex, "Failed to dispatch outbox message {OutboxMessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishOutboxMessageAsync(
        IIntegrationEventPublisher publisher,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
            if (outboxMessage.EventType == typeof(OrderClassificationIntegrationEvent).FullName)
        {
            var integrationEvent = JsonSerializer.Deserialize<OrderClassificationIntegrationEvent>(outboxMessage.Payload)
                ?? throw new InvalidOperationException($"Could not deserialize {outboxMessage.EventType}.");

            await publisher.PublishAsync(integrationEvent, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Unsupported outbox event type {outboxMessage.EventType}.");
    }
}



