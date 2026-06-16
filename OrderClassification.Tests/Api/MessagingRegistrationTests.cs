using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderClassification.Application.Messaging;
using OrderClassification.Infrastructure;
using OrderClassification.Infrastructure.Messaging;

namespace OrderClassification.Tests.Api;

public class MessagingRegistrationTests
{
    [Fact]
    public void AddInfrastructureServices_UsesInMemoryPublisher_WhenProviderIsInMemory()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:OrderClassification"] = "Data Source=:memory:",
            ["Messaging:Provider"] = "InMemory"
        });

        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IIntegrationEventPublisher>();

        Assert.IsType<InMemoryEventPublisher>(publisher);
    }

    [Fact]
    public void AddInfrastructureServices_FallsBackToInMemory_WhenServiceBusProviderHasNoConnectionString()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:OrderClassification"] = "Data Source=:memory:",
            ["Messaging:Provider"] = "ServiceBus",
            ["Messaging:ServiceBus:ConnectionString"] = string.Empty
        });

        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IIntegrationEventPublisher>();

        Assert.IsType<InMemoryEventPublisher>(publisher);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}

