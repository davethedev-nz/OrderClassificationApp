namespace OrderClassification.Application.Messaging;

/// <summary>
/// Application-facing abstraction for persisting outgoing integration events into the outbox.
/// Infrastructure owns the storage details.
/// </summary>
public interface IOutboxWriter
{
    Task EnqueueAsync(string eventType, string payload, string? correlationId, CancellationToken cancellationToken = default);
}

