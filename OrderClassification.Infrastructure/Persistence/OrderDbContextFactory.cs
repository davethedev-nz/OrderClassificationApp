using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef` to create the DbContext and run migrations.
///
/// Python analogy: a small script/config object that tells Alembic what database
/// connection and models to use during migration generation.
/// </summary>
public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__OrderClassification");
        var connectionString = string.IsNullOrWhiteSpace(fromEnvironment)
            ? "Server=localhost;Database=orderclassification;Trusted_Connection=True;Encrypt=False;"
            : fromEnvironment;

        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new OrderDbContext(optionsBuilder.Options);
    }
}

