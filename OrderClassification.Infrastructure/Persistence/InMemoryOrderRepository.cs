using OrderClassification.Domain.Orders;

namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// In-memory repository implementation — temporary stub until Day 3 adds EF Core.
///
/// Python analogy: a dict-based fake repository you'd use in unit tests
/// before wiring a real SQLAlchemy session. Same principle here, but it
/// lets the whole stack run without a DB on Day 1.
/// </summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _store = [];

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<Order?> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(o => o.ReferenceNumber == referenceNumber));

    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Order>>(_store.Values.ToList().AsReadOnly());

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask; // no-op until EF Core is wired
    
    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }
}

