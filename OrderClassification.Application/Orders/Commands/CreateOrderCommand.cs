namespace OrderClassification.Application.Orders.Commands;

/// <summary>
/// Command DTO for creating a new order.
///
/// Commands represent intent to change state.
/// They are immutable (records) and validated before reaching the domain.
///
/// Python analogy: a Pydantic model used as the body of a POST request.
/// </summary>
public sealed record CreateOrderCommand(string ReferenceNumber);

