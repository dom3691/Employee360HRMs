using Employee360.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>
/// Base controller: exposes the MediatR sender and maps <see cref="Result"/> /
/// <see cref="Result{T}"/> outcomes to HTTP responses (failures become RFC 7807
/// Problem Details with the business errors listed).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    /// <summary>The MediatR sender resolved from request services.</summary>
    protected ISender Sender =>
        _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Maps a non-generic result: 204 on success, 400 problem on failure.</summary>
    /// <param name="result">The handler outcome.</param>
    protected IActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : FailureProblem(result);

    /// <summary>Maps a generic result: 200 with the value, or 400 problem on failure.</summary>
    /// <typeparam name="T">The success payload type.</typeparam>
    /// <param name="result">The handler outcome.</param>
    protected IActionResult FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : FailureProblem(result);

    private ObjectResult FailureProblem(Result result)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "The request could not be processed.",
            Status = StatusCodes.Status400BadRequest,
            Detail = result.Error,
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["errors"] = result.Errors;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return BadRequest(problem);
    }
}
