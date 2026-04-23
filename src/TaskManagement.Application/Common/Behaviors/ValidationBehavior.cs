using ErrorOr;
using FluentValidation;
using Mediator;

namespace TaskManagement.Application.Common.Behaviors;

// Runs all registered FluentValidation validators for TRequest before the handler.
// Constrained to IErrorOr so aggregated validation errors can be returned as an
// ErrorOr<T> without throwing — keeping expected failures out of the exception path.
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
    where TResponse : IErrorOr
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TRequest, TResponse> next)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(message);
        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .Distinct()
            .ToList();

        if (failures.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        // ErrorOr<T> has an implicit conversion from List<Error> — cast via dynamic
        // so this behavior works for any TResponse : IErrorOr without extra plumbing.
        return (TResponse)(dynamic)failures;
    }
}
