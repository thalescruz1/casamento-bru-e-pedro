using System.Reflection;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Common;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var activeValidators = validators.ToArray();
        if (activeValidators.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<string>();

        foreach (var validator in activeValidators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors.Select(e => e.ErrorMessage));
            }
        }

        if (failures.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var message = string.Join("; ", failures);
        return CreateFailure(message)
            ?? throw new ValidationException(message);
    }

    private static TResponse? CreateFailure(string message)
    {
        var type = typeof(TResponse);
        if (type == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(Error.Validation(message));
        }

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return default;
        }

        var failureMethod = type.GetMethod(nameof(Result<object>.Failure), BindingFlags.Static | BindingFlags.Public);
        if (failureMethod is null)
        {
            return default;
        }

        return (TResponse?)failureMethod.Invoke(null, new object[] { Error.Validation(message) });
    }
}
