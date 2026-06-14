using System.ComponentModel.DataAnnotations;

namespace OrderClassification.Api.Validation;

/// <summary>
/// Validates endpoint arguments that use DataAnnotations.
///
/// Minimal APIs do not auto-run MVC model validation the same way controllers do,
/// so this filter provides a standard, explicit validation step for request DTOs.
///
/// Python analogy: a FastAPI dependency that validates a request model before the
/// service handler runs.
/// </summary>
public sealed class DataAnnotationsValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validationErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            if (argument is string or Guid or DateTime or DateTimeOffset or CancellationToken)
            {
                continue;
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(argument);
            var isValid = Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true);

            if (isValid)
            {
                continue;
            }

            foreach (var result in validationResults)
            {
                var memberNames = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
                foreach (var memberName in memberNames)
                {
                    var key = string.IsNullOrWhiteSpace(memberName) ? argument.GetType().Name : memberName;
                    if (!validationErrors.TryGetValue(key, out var existing))
                    {
                        validationErrors[key] = [result.ErrorMessage ?? "Invalid value."];
                    }
                    else
                    {
                        validationErrors[key] = [.. existing, result.ErrorMessage ?? "Invalid value."];
                    }
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        return await next(context);
    }
}

