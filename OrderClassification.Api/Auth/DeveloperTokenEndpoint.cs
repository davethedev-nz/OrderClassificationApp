namespace OrderClassification.Api.Auth;

public static class DeveloperTokenEndpoint
{
    public static IEndpointConventionBuilder MapDeveloperTokenEndpoint(this IEndpointRouteBuilder routes)
    {
        return routes.MapGet("/dev/token", (TokenService tokenService) => Results.Ok(new { token = tokenService.CreateDeveloperToken() }))
            .AllowAnonymous()
            .WithName("GetDeveloperToken")
            .WithSummary("Issue a developer JWT for local testing");
    }
}

