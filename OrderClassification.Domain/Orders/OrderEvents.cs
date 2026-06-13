using OrderClassification.Domain.Common;

namespace OrderClassification.Domain.Orders;

/// <summary>
/// Raised when a new order enters the system.
/// Downstream consumers (Day 5) will react to this event.
/// </summary>
public sealed record OrderCreatedEvent(Guid OrderId, string ReferenceNumber) : IDomainEvent;

/// <summary>
/// Raised when an order has been given a classification label.
/// </summary>
public sealed record OrderClassifiedEvent(Guid OrderId, string Classification) : IDomainEvent;

