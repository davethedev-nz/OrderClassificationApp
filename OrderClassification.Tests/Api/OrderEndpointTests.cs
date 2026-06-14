using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using OrderClassification.Tests.TestInfrastructure;

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
public class OrderEndpointTests(SqliteWebApplicationFactory factory)
    : IClassFixture<SqliteWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Root_Returns200()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
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
        await AuthenticateAsync();

        // Act - create
        var createResponse = await _client.PostAsJsonAsync("/orders", command);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.True(createResponse.Headers.Contains("X-Correlation-ID"));

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedOrderResponse>();
        Assert.NotNull(created);

        // Act - retrieve
        var getResponse = await _client.GetAsync($"/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Get_OrderByUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Order_WithInvalidPayload_ReturnsValidationProblem()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/orders", new { ReferenceNumber = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Contains("ReferenceNumber", problem.Errors.Keys);
    }

    [Fact]
    public async Task Post_Order_WithDuplicateReference_ReturnsConflict()
    {
        var command = new { ReferenceNumber = "DUP-001" };
        await AuthenticateAsync();

        var first = await _client.PostAsJsonAsync("/orders", command);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/orders", command);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Post_Order_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { ReferenceNumber = "NOAUTH-001" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        var tokenResponse = await _client.GetFromJsonAsync<DeveloperTokenResponse>("/dev/token");
        Assert.NotNull(tokenResponse);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse!.Token);
    }

    private sealed record CreatedOrderResponse(Guid Id);

    private sealed record DeveloperTokenResponse(string Token);
}

