using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TaskManagement.Api.Localization;
using TaskManagement.Api.Resources;

namespace TaskManagement.Api.Infrastructure;

// Centralised translation from an ErrorOr<T> handler result to a typed
// Minimal-API IResult, returning RFC 7807 ProblemDetails on failure.
//
// The ProblemDetails text is localised at the API edge:
//   - titles come from ApiResource (culture-aware IStringLocalizer<ApiResource>)
//   - per-error details come from SharedResource via IErrorLocalizer, keyed by
//     the stable domain Error.Code (e.g. "Task.NotFound")
// The request culture is set by UseRequestLocalization (Accept-Language header,
// cookie, or ?culture= query string).
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

        var errorLocalizer = httpContext.RequestServices.GetRequiredService<IErrorLocalizer>();
        var apiLocalizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<ApiResource>>();

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            // Each FluentValidation failure arrives with the offending property
            // name as the Code; localise the description so the per-field error
            // bag is in the user's language.
            var modelState = errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => errorLocalizer.Localize(e)).ToArray());
            return Results.ValidationProblem(
                modelState,
                title: apiLocalizer["ProblemTitle.Validation"],
                instance: httpContext.Request.Path);
        }

        var first = errors[0];
        var (status, titleKey) = first.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "ProblemTitle.NotFound"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "ProblemTitle.Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "ProblemTitle.Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "ProblemTitle.Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "ProblemTitle.ServerError"),
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = apiLocalizer[titleKey],
            Detail = errorLocalizer.Localize(first),
            Type = first.Code,
            Instance = httpContext.Request.Path,
        };

        return Results.Problem(problem);
    }
}
