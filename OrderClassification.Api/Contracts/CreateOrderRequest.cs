using System.ComponentModel.DataAnnotations;

namespace OrderClassification.Api.Contracts;

/// <summary>
/// Transport contract for creating an order.
///
/// This lives in the API layer because it is part of the HTTP boundary.
/// It is intentionally separate from the Application command model.
///
/// Python analogy: a FastAPI request schema or Django form/serializer
/// that is separate from the service-layer input object.
/// </summary>
public sealed record CreateOrderRequest
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string ReferenceNumber { get; init; } = string.Empty;
}

