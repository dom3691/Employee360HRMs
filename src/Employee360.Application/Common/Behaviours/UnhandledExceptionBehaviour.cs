using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee360.Application.Common.Behaviours;

/// <summary>
/// Outermost MediatR pipeline behaviour: logs any unhandled exception with the
/// request name and payload before rethrowing, so the API middleware can return
/// an RFC 7807 response while the full context lands in Serilog / App Insights.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public sealed class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehaviour(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (FluentValidation.ValidationException)
        {
            // Expected control flow for non-Result handlers; middleware maps to 400.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in {RequestName}: {@Request}",
                typeof(TRequest).Name,
                request);

            throw;
        }
    }
}
