namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// Read model built from OrderClassificationIntegrationEvent.
/// Maintains an idempotency key to prevent duplicate processing.
/// </summary>
public sealed class ClassificationReadModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid OrderId { get; set; }
    
    public string Classification { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique key per integration event; prevents reprocessing the same event.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    
    public DateTime PublishedAt { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

