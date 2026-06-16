namespace OrderClassification.Infrastructure.Messaging;

public interface IOutboxDispatcher
{
    Task DispatchAsync(CancellationToken cancellationToken = default);
}

