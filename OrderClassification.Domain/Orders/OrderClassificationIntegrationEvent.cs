namespace OrderClassification.Domain.Orders;

public sealed record OrderClassificationIntegrationEvent(
    Guid OrderId,
    string Classification,
    string CorrelationId,
    Guid EventId,
    DateTime PublishedAt,
    string IdempotencyKey
);

