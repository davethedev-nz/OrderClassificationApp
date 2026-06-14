using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderClassification.Domain.Orders;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Infrastructure;

/// <summary>
/// Infrastructure wiring for persistence.
///
/// Python analogy: this is where we bind the SQLAlchemy engine/session
/// to the repository implementation.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderClassification")
            ?? "Data Source=orderclassification.db";

        services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IOrderRepository, EfOrderRepository>();

        return services;
    }
}

