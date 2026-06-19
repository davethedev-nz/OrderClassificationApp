using OrderClassification.Api.Contracts;
using OrderClassification.Api.Auth;
using OrderClassification.Api.Exceptions;
using OrderClassification.Api.Middleware;
using OrderClassification.Api.Validation;
using OrderClassification.Application;
using OrderClassification.Application.Orders.Commands;
using OrderClassification.Application.Orders.Queries;
using OrderClassification.Infrastructure;
using OrderClassification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Service Registration
// ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAuthServices(builder.Configuration);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    if (string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
    {
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }
}

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
app.UseAuthentication();
app.UseAuthorization();

var httpsPort = app.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!string.IsNullOrWhiteSpace(httpsPort))
{
    app.UseHttpsRedirection();
}

// ---------------------------------------------------------------
// Endpoint Mapping
// ---------------------------------------------------------------
app.MapHealthChecks("/health");
app.MapDeveloperTokenEndpoint();

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

orders.MapPost("", [Authorize(Policy = "OrdersWriter")] async (CreateOrderRequest request, CreateOrderHandler handler, CancellationToken ct) =>
{
    var command = new CreateOrderCommand(request.ReferenceNumber);
    var id = await handler.HandleAsync(command, ct);
    return Results.Created($"/orders/{id}", new { id });
})
.WithName("CreateOrder")
.WithSummary("Create a new order");

orders.MapPatch("/classify", [Authorize(Policy = "OrdersWriter")] async (ClassifyOrderRequest request, ClassifyOrderHandler handler, HttpContext context, CancellationToken ct) =>
{
    var correlationId = context.Items[RequestCorrelationMiddleware.ItemKey]?.ToString() ?? "unknown";
    var command = new ClassifyOrderCommand(request.Id, request.Classification, request.IdempotencyKey ?? "");
    await handler.HandleAsync(command, correlationId, ct);
    return Results.Ok($"Order {request.Id} Classified");
})
.WithName("ClassifyOrder")
.WithSummary("Classify an order with a given classification");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
