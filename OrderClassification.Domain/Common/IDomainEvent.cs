namespace OrderClassification.Domain.Common;

/// <summary>
/// Marker interface for domain events.
///
/// Domain events represent something that happened inside the domain boundary.
/// They are raised inside entities and dispatched AFTER the transaction commits.
///
/// Python analogy: signals in Django, or events you'd publish to an internal
/// event bus (not Kafka yet — those are integration events, Day 5).
/// </summary>
public interface IDomainEvent;

