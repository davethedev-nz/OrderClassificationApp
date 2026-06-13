using OrderClassification.Domain.Orders;

namespace OrderClassification.Domain.Orders;

/// <summary>
/// Repository interface for Orders — defined in Domain, implemented in Infrastructure.
///
/// This is the Dependency Inversion Principle in action:
/// Domain defines the contract, Infrastructure satisfies it.
///
/// Python analogy: an abstract base class / Protocol your domain
/// defines, and your SQLAlchemy repo implements.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

