using OrderClassification.Domain.Orders;

namespace OrderClassification.Application.Orders.Commands;

/// <summary>
/// Handles the CreateOrderCommand use-case.
///
/// Application layer orchestrates domain objects and infrastructure.
/// It does NOT contain business rules — those live in the domain.
///
/// Python analogy: a service layer function or a FastAPI route handler
/// that calls domain model methods and persists via a repository.
/// </summary>
public sealed class CreateOrderHandler(IOrderRepository repository)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = Order.Create(command.ReferenceNumber);

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}

