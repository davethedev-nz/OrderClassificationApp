using Microsoft.Extensions.Logging;
using OrderClassification.Application.Messaging;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Infrastructure.Messaging;

public sealed class EfOutboxWriter(
    OrderDbContext dbContext,
    ILogger<EfOutboxWriter> logger)
    : IOutboxWriter
{
    public async Task EnqueueAsync(string eventType, string payload, string? correlationId, CancellationToken cancellationToken = default)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = eventType,
            Payload = payload,
            CorrelationId = correlationId,
            Status = "Pending",
            OccurredAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Enqueued outbox message for {EventType}", eventType);
    }
}

