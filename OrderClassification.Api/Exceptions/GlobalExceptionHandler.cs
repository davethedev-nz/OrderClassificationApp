using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using OrderClassification.Api.Middleware;

namespace OrderClassification.Api.Exceptions;

/// <summary>
/// Centralized exception-to-ProblemDetails translation.
///
/// This is the .NET equivalent of a global exception middleware in FastAPI/Django.
/// We keep the mapping explicit so interviewers can see the operational choices.
///
/// Mapping used here:
/// - ArgumentException -> 400 Bad Request
/// - InvalidOperationException -> 409 Conflict
/// - everything else -> 500 Internal Server Error
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Validation or argument error"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Invalid operation"),
            DbUpdateException dbUpdateException when IsUniqueConstraintViolation(dbUpdateException)
                => (StatusCodes.Status409Conflict, "Conflict while saving changes"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        logger.LogError(exception, "Unhandled exception mapped to {StatusCode}", statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "The request could not be completed." : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (httpContext.Items.TryGetValue(RequestCorrelationMiddleware.ItemKey, out var correlationId) && correlationId is not null)
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException sqlException)
        {
            return sqlException.Number is 2601 or 2627;
        }

        if (exception.InnerException is not null && exception.InnerException.GetType().Name == "SqliteException")
        {
            return exception.InnerException.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}


