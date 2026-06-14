namespace OrderClassification.Application.Orders.Commands;

public sealed record ClassifyOrderCommand(
    Guid Id,
    string Classification,
    string IdempotencyKey = ""  // allows safe retry of classification requests
);
