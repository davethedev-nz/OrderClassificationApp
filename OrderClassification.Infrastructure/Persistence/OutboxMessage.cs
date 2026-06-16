namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// Minimal outbox row used to persist outgoing integration events until they are dispatched.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public string? Error { get; set; }
}

