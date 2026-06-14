using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderClassification.Domain.Orders;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Infrastructure.Messaging;

/// <summary>
/// Handles OrderClassificationIntegrationEvent by storing it in the read model.
/// Uses idempotency key to ensure exactly-once processing.
/// </summary>
public sealed class OrderClassificationEventHandler(
    OrderDbContext dbContext,
    ILogger<OrderClassificationEventHandler> logger)
    : IEventHandler<OrderClassificationIntegrationEvent>
{
    public async Task HandleAsync(
        OrderClassificationIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        // Check for idempotency: have we already processed this event?
        var existing = await dbContext.ClassificationReadModels
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == @event.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "Skipping duplicate event: {IdempotencyKey}",
                @event.IdempotencyKey
            );
            return;
        }

        // First time seeing this event: process it
        var readModel = new ClassificationReadModel
        {
            OrderId = @event.OrderId,
            Classification = @event.Classification,
            IdempotencyKey = @event.IdempotencyKey,
            PublishedAt = @event.PublishedAt,
            ProcessedAt = DateTime.UtcNow
        };

        dbContext.ClassificationReadModels.Add(readModel);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Processed OrderClassificationIntegrationEvent: OrderId={OrderId}, Classification={Classification}",
            @event.OrderId,
            @event.Classification
        );
    }
}

