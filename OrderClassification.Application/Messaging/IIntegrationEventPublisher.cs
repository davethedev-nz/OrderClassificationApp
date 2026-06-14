namespace OrderClassification.Application.Messaging;

/// <summary>
/// Application-facing abstraction for publishing integration events.
/// Infrastructure provides the concrete transport implementation.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}

