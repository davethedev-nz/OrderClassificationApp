namespace OrderClassification.Application.Orders.Commands;


public sealed record ClassifyOrderCommand(Guid Id, string Classification);
