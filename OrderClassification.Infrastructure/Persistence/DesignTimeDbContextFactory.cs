// ...existing code...
using Microsoft.EntityFrameworkCore.Design;

namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// Design-time EF Core factory used by dotnet-ef for migrations.
/// This file intentionally mirrors the runtime database shape used in the app.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
        => new OrderDbContextFactory().CreateDbContext(args);
}
// ...existing code...

