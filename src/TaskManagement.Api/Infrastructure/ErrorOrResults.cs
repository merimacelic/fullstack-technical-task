using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.Api.Infrastructure;

// Centralised translation from an ErrorOr<T> handler result to a typed
// Minimal-API IResult, returning RFC 7807 ProblemDetails on failure.
public static class ErrorOrResults
{
    public static IResult ToOk<TValue>(this ErrorOr<TValue> result, HttpContext httpContext)
        => result.Match<IResult>(
            value => Results.Ok(value),
            errors => ToProblem(errors, httpContext));

    public static IResult ToCreated<TValue>(
        this ErrorOr<TValue> result,
        HttpContext httpContext,
        Func<TValue, string> locationBuilder)
        => result.Match<IResult>(
            value => Results.Created(locationBuilder(value), value),
            errors => ToProblem(errors, httpContext));

    public static IResult ToNoContent<TValue>(this ErrorOr<TValue> result, HttpContext httpContext)
        => result.Match<IResult>(
            _ => Results.NoContent(),
            errors => ToProblem(errors, httpContext));

    public static IResult ToProblem(IReadOnlyList<Error> errors, HttpContext httpContext)
    {
        if (errors.Count == 0)
        {
            return Results.Problem();
        }

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var modelState = errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray());
            return Results.ValidationProblem(
                modelState,
                instance: httpContext.Request.Path);
        }

        var first = errors[0];
        var (status, title) = first.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = first.Description,
            Type = first.Code,
            Instance = httpContext.Request.Path,
        };

        return Results.Problem(problem);
    }
}
