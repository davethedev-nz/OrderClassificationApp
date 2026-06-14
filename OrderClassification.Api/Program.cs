using OrderClassification.Api.Contracts;
using OrderClassification.Api.Exceptions;
using OrderClassification.Api.Middleware;
using OrderClassification.Api.Validation;
using OrderClassification.Application;
using OrderClassification.Application.Orders.Commands;
using OrderClassification.Application.Orders.Queries;
using OrderClassification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Service Registration
// ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

app.UseExceptionHandler();
app.UseMiddleware<RequestCorrelationMiddleware>();

var httpsPort = app.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!string.IsNullOrWhiteSpace(httpsPort))
{
    app.UseHttpsRedirection();
}

// ---------------------------------------------------------------
// Endpoint Mapping
// ---------------------------------------------------------------
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new { service = "OrderClassification.Api", status = "running" }))
    .WithName("Root")
    .WithSummary("Service liveness ping");

var orders = app.MapGroup("/orders")
    .AddEndpointFilter(new DataAnnotationsValidationFilter());

// Orders endpoints
orders.MapGet("", async (GetOrderHandler handler, CancellationToken ct) =>
{
    var orders = await handler.HandleAllAsync(ct);
    return Results.Ok(orders);
})
.WithName("GetOrders")
.WithSummary("List all orders");

orders.MapGet("/{id:guid}", async (Guid id, GetOrderHandler handler, CancellationToken ct) =>
{
    var order = await handler.HandleAsync(id, ct);
    return order is null ? Results.NotFound() : Results.Ok(order);
})
.WithName("GetOrderById")
.WithSummary("Get a single order by ID");

orders.MapPost("", async (CreateOrderRequest request, CreateOrderHandler handler, CancellationToken ct) =>
{
    var command = new CreateOrderCommand(request.ReferenceNumber);
    var id = await handler.HandleAsync(command, ct);
    return Results.Created($"/orders/{id}", new { id });
})
.WithName("CreateOrder")
.WithSummary("Create a new order");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
