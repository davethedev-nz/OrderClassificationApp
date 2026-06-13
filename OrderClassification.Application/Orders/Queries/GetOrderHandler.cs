using OrderClassification.Domain.Orders;

namespace OrderClassification.Application.Orders.Queries;

/// <summary>
/// Handles read-side queries for orders.
/// </summary>
public sealed class GetOrderHandler(IOrderRepository repository)
{
    public async Task<OrderDto?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : MapToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> HandleAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = await repository.GetAllAsync(cancellationToken);
        return orders.Select(MapToDto).ToList().AsReadOnly();
    }

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.ReferenceNumber,
        order.Status.ToString(),
        order.Classification,
        order.CreatedAt,
        order.ClassifiedAt
    );
}

