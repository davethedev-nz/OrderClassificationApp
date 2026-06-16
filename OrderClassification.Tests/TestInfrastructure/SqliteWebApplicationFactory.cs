using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderClassification.Infrastructure.Persistence;

namespace OrderClassification.Tests.TestInfrastructure;

/// <summary>
/// Integration-test host that swaps the production SQLite file connection
/// for an isolated in-memory SQLite database.
///
/// Python analogy: a pytest fixture that opens an in-memory SQLite database
/// and injects it into the app for each test class/session.
/// </summary>
public sealed class SqliteWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Provider"] = "InMemory",
                ["Messaging:ServiceBus:ConnectionString"] = string.Empty
            });
        });

        builder.ConfigureServices(services =>
        {
            RemoveExistingDbContextRegistrations(services);

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<OrderDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }

    private static void RemoveExistingDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>) || d.ServiceType == typeof(OrderDbContext))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}


