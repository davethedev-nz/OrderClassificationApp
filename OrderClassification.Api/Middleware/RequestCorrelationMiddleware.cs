namespace OrderClassification.Api.Middleware;

/// <summary>
/// Adds or propagates a request correlation ID.
///
/// Think of this like a tracing/request-id middleware in FastAPI/Django.
/// We keep it simple and explicit so interview discussions stay concrete.
///
/// The correlation ID is:
/// - read from `X-Correlation-ID` if the client supplied one
/// - otherwise generated as a new GUID
/// - returned on the response header
/// - stored in HttpContext.Items for later access by logging / exception handling
/// </summary>
public sealed class RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceIdentifier"] = context.TraceIdentifier,
            ["Path"] = context.Request.Path.Value ?? string.Empty,
            ["Method"] = context.Request.Method
        }))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            logger.LogInformation("Handling request {Method} {Path}", context.Request.Method, context.Request.Path);

            try
            {
                await next(context);
                logger.LogInformation(
                    "Completed request {Method} {Path} with {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unhandled exception for request {Method} {Path} after {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}

