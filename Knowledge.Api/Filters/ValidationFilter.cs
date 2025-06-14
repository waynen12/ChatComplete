using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.Api.Filters;

/// <summary>
/// This filter validates incoming request data against the object model.
/// </summary>
public sealed class ValidationFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        // Walk through all arguments passed to the endpoint
        foreach (var arg in ctx.Arguments)
        {
            if (arg is null) continue;

            var context = new ValidationContext(arg);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(arg, context, results, validateAllProperties: true))
            {
                // Convert ValidationResult list → ProblemDetails dictionary
                var errors = results
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage!).ToArray());

                return Results.ValidationProblem(errors); // RFC 7807 JSON, status 400
            }
        }

        // All DTOs valid → continue down the pipeline
        return await next(ctx);
    }
}