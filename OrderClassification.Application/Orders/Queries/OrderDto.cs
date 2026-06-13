namespace OrderClassification.Application.Orders.Queries;

/// <summary>
/// Response DTO for an Order query.
///
/// This is NOT the domain entity — it's a read model/projection.
/// Reason: domain entities carry behavior and rules. Exposing them
/// directly couples the API contract to internal model changes.
///
/// Python analogy: a Pydantic response_model separate from the ORM model.
/// </summary>
public sealed record OrderDto(
    Guid Id,
    string ReferenceNumber,
    string Status,
    string? Classification,
    DateTime CreatedAt,
    DateTime? ClassifiedAt
);

