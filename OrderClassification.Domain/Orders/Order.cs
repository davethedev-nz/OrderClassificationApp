using OrderClassification.Domain.Common;

namespace OrderClassification.Domain.Orders;

/// <summary>
/// Order aggregate root.
///
/// An aggregate root is the entry point to a cluster of domain objects.
/// Nothing outside can hold references to inner objects directly —
/// all mutations go through the root.
///
/// Python analogy: a "manager-level" model that owns its related child models
/// and enforces business rules before persisting.
/// </summary>
public sealed class Order : Entity
{
    public string ReferenceNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public string? Classification { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClassifiedAt { get; private set; }

    // EF Core requires a parameterless constructor (private is fine)
    private Order() { }

    public static Order Create(string referenceNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceNumber);

        var order = new Order
        {
            ReferenceNumber = referenceNumber,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, referenceNumber));
        return order;
    }

    public void Classify(string classification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classification);

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Order {Id} cannot be classified in status {Status}.");

        Classification = classification;
        Status = OrderStatus.Classified;
        ClassifiedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderClassifiedEvent(Id, classification));
    }
}

