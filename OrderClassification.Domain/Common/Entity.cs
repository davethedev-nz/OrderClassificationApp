namespace OrderClassification.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Every entity has a unique identity (Id) — this is what separates
/// an entity from a value object in DDD.
///
/// Python analogy: a SQLAlchemy Base model with an id column,
/// but the domain version has no ORM coupling.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

