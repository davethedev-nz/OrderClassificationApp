using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OrderClassification.Tests.Api;

/// <summary>
/// Integration tests that spin up the full ASP.NET Core pipeline in-process.
///
/// Python analogy: pytest with httpx.AsyncClient and a FastAPI TestClient,
/// or Django's Client test class — same idea, runs the full stack in memory.
///
/// WebApplicationFactory uses the `public partial class Program` declaration
/// in Program.cs as its entry point — that's why we added it.
/// </summary>
public class OrderEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Root_Returns200()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Health_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Order_ThenGet_ReturnsCreatedOrder()
    {
        // Arrange
        var command = new { ReferenceNumber = "INT-TEST-001" };

        // Act - create
        var createResponse = await _client.PostAsJsonAsync("/orders", command);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedOrderResponse>();
        Assert.NotNull(created);

        // Act - retrieve
        var getResponse = await _client.GetAsync($"/orders/{created!.id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Get_OrderByUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CreatedOrderResponse(Guid id);
}

