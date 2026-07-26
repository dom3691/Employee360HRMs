using System.Diagnostics;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee360.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs every request with the acting user,
/// execution time, and outcome (structured via Serilog). Requests slower than
/// 3 seconds are logged as warnings (performance budget, PRD NFR-PERF-001).
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 3000;

    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "Handling {RequestName} for user {UserId}",
            requestName,
            userId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (response is Result { IsFailure: true } failedResult)
        {
            _logger.LogWarning(
                "{RequestName} completed with business failure in {ElapsedMs}ms for user {UserId}: {Errors}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                userId,
                string.Join("; ", failedResult.Errors));
        }
        else if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "{RequestName} completed in {ElapsedMs}ms (over {ThresholdMs}ms budget) for user {UserId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                SlowRequestThresholdMs,
                userId);
        }
        else
        {
            _logger.LogInformation(
                "{RequestName} completed in {ElapsedMs}ms for user {UserId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                userId);
        }

        return response;
    }
}
