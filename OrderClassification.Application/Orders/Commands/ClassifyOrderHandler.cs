using OrderClassification.Domain.Orders;

namespace OrderClassification.Application.Orders.Commands;

public sealed class ClassifyOrderHandler(IOrderRepository repository)
{
    public async Task<Guid> HandleAsync(ClassifyOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(command.Id, cancellationToken);
        
        if (order is null) 
            throw new InvalidOperationException($"Order with id {command.Id} not found");
        
        order.Classify(command.Classification);
        await repository.UpdateAsync(order, cancellationToken);
        
        await repository.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}