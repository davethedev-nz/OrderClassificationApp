namespace OrderClassification.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string Provider { get; set; } = Providers.InMemory;
    public ServiceBusOptions ServiceBus { get; set; } = new();
    public OutboxOptions Outbox { get; set; } = new();

    public bool UseServiceBus() => string.Equals(Provider, Providers.ServiceBus, StringComparison.OrdinalIgnoreCase);

    public static class Providers
    {
        public const string InMemory = "InMemory";
        public const string ServiceBus = "ServiceBus";
    }
}

public sealed class ServiceBusOptions
{
    public string? ConnectionString { get; set; }
    public string TopicName { get; set; } = "order-classification";
    public string SubscriptionName { get; set; } = "classification-read-model";
    public string OrderClassifiedSubject { get; set; } = "orders.classified";
    public int MaxConcurrentCalls { get; set; } = 1;
}

public sealed class OutboxOptions
{
    public bool Enabled { get; set; } = true;
    public int DispatchIntervalMilliseconds { get; set; } = 250;
    public int BatchSize { get; set; } = 25;
}


