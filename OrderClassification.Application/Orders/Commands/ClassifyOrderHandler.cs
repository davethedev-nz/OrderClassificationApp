using OrderClassification.Application.Services;
using OrderClassification.Domain.Orders;

namespace OrderClassification.Application.Orders.Commands;

public sealed class ClassifyOrderHandler(
    IOrderRepository repository,
    DomainEventPublisher domainEventPublisher)
{
    public async Task<Guid> HandleAsync(
        ClassifyOrderCommand command,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(command.Id, cancellationToken);
        
        if (order is null) 
            throw new InvalidOperationException($"Order with id {command.Id} not found");
        
        order.Classify(command.Classification);
        await repository.UpdateAsync(order, cancellationToken);
        
        await repository.SaveChangesAsync(cancellationToken);

        // Publish domain events as integration events after successful persistence
        await domainEventPublisher.PublishAsync(
            order.DomainEvents,
            correlationId,
            command.IdempotencyKey,
            cancellationToken);

        order.ClearDomainEvents();

        return order.Id;
    }
}