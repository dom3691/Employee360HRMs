using Employee360.Domain.Common;
using FluentValidation;
using MediatR;

namespace Employee360.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered FluentValidation validators
/// before the handler executes. When validation fails:
/// <list type="bullet">
/// <item>Handlers returning <see cref="Result"/> / <see cref="Result{T}"/> receive a
/// failed result carrying the error messages (business-rule convention).</item>
/// <item>Any other response type falls back to throwing
/// <see cref="ValidationException"/>, which the API middleware translates into an
/// RFC 7807 response (HTTP 400).</item>
/// </list>
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var errors = failures.Select(f => f.ErrorMessage).Distinct().ToArray();

        if (TryCreateFailureResult(errors, out var failureResult))
        {
            return failureResult;
        }

        throw new ValidationException(failures);
    }

    private static bool TryCreateFailureResult(string[] errors, out TResponse result)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            result = (TResponse)(object)Result.Failure(errors);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(
                nameof(Result.Failure),
                [typeof(string[])])!;

            result = (TResponse)failureMethod.Invoke(null, [errors])!;
            return true;
        }

        result = default!;
        return false;
    }
}
