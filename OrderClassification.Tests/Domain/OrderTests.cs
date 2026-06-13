using OrderClassification.Domain.Orders;

namespace OrderClassification.Tests.Domain;

/// <summary>
/// Unit tests for the Order aggregate.
///
/// These test pure domain logic — no DB, no HTTP, no DI container.
/// Python analogy: pytest unit tests against your model/service layer
/// with no database or request context.
/// </summary>
public class OrderTests
{
    [Fact]
    public void Create_ShouldSetPendingStatus_AndRaiseEvent()
    {
        // Arrange + Act
        var order = Order.Create("ORD-001");

        // Assert
        Assert.Equal("ORD-001", order.ReferenceNumber);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.DomainEvents);
        Assert.IsType<OrderCreatedEvent>(order.DomainEvents.First());
    }

    [Fact]
    public void Classify_ShouldSetClassification_AndRaiseEvent()
    {
        // Arrange
        var order = Order.Create("ORD-002");
        order.ClearDomainEvents();

        // Act
        order.Classify("High-Value");

        // Assert
        Assert.Equal("High-Value", order.Classification);
        Assert.Equal(OrderStatus.Classified, order.Status);
        Assert.Single(order.DomainEvents);
        Assert.IsType<OrderClassifiedEvent>(order.DomainEvents.First());
    }

    [Fact]
    public void Classify_AlreadyClassified_ShouldThrow()
    {
        // Arrange
        var order = Order.Create("ORD-003");
        order.Classify("Standard");

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => order.Classify("Premium"));
    }

    [Fact]
    public void Create_WithEmptyReference_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Order.Create(""));
    }
}

