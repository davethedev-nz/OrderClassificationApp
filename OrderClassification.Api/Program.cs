using OrderClassification.Application;
using OrderClassification.Infrastructure;
using OrderClassification.Application.Orders.Commands;
using OrderClassification.Application.Orders.Queries;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Service Registration
// ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ---------------------------------------------------------------
// Endpoint Mapping
// ---------------------------------------------------------------
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new { service = "OrderClassification.Api", status = "running" }))
    .WithName("Root")
    .WithSummary("Service liveness ping");

// Orders endpoints
app.MapGet("/orders", async (GetOrderHandler handler, CancellationToken ct) =>
{
    var orders = await handler.HandleAllAsync(ct);
    return Results.Ok(orders);
})
.WithName("GetOrders")
.WithSummary("List all orders");

app.MapGet("/orders/{id:guid}", async (Guid id, GetOrderHandler handler, CancellationToken ct) =>
{
    var order = await handler.HandleAsync(id, ct);
    return order is null ? Results.NotFound() : Results.Ok(order);
})
.WithName("GetOrderById")
.WithSummary("Get a single order by ID");

app.MapPost("/orders", async (CreateOrderCommand command, CreateOrderHandler handler, CancellationToken ct) =>
{
    var id = await handler.HandleAsync(command, ct);
    return Results.Created($"/orders/{id}", new { id });
})
.WithName("CreateOrder")
.WithSummary("Create a new order");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
