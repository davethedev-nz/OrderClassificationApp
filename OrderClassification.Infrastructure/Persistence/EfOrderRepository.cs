using Microsoft.EntityFrameworkCore;
using OrderClassification.Domain.Orders;

namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// EF Core repository implementation.
///
/// Python analogy: a repository that uses a SQLAlchemy Session and commits
/// through a unit-of-work pattern.
/// </summary>
public sealed class EfOrderRepository(OrderDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<Order?> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default) =>
        dbContext.Orders.FirstOrDefaultAsync(order => order.ReferenceNumber == referenceNumber, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await dbContext.Orders.AddAsync(order, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

