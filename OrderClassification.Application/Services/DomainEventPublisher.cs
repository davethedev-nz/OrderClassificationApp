using OrderClassification.Application.Messaging;
using OrderClassification.Domain.Common;
using OrderClassification.Domain.Orders;

namespace OrderClassification.Application.Services;

/// <summary>
/// Translates domain events to integration events and publishes them.
/// Domain events are internal signals; integration events are the external bus contract.
/// </summary>
public sealed class DomainEventPublisher(
    IIntegrationEventPublisher eventPublisher)
{
    public async Task PublishAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        string correlationId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            switch (domainEvent)
            {
                case OrderClassifiedEvent classifiedEvent:
                    await PublishOrderClassifiedAsync(classifiedEvent, correlationId, idempotencyKey, cancellationToken);
                    break;
            }
        }
    }

    private async Task PublishOrderClassifiedAsync(
        OrderClassifiedEvent domainEvent,
        string correlationId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderClassificationIntegrationEvent(
            OrderId: domainEvent.OrderId,
            Classification: domainEvent.Classification,
            CorrelationId: correlationId,
            EventId: Guid.NewGuid(),
            PublishedAt: DateTime.UtcNow,
            IdempotencyKey: string.IsNullOrWhiteSpace(idempotencyKey)
                ? $"{domainEvent.OrderId:N}:{domainEvent.Classification}"
                : idempotencyKey
        );

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
