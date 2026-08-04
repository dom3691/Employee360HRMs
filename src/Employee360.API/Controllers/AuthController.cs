using Employee360.Application.Features.Auth.ChangePassword;
using Employee360.Application.Features.Auth.ForgotPassword;
using Employee360.Application.Features.Auth.Login;
using Employee360.Application.Features.Auth.RefreshToken;
using Employee360.Application.Features.Auth.ResetPassword;
using Employee360.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Employee360.API.Controllers;

/// <summary>Authentication endpoints (FR-AUTH-001..004).</summary>
[Route("api/v1/auth")]
[EnableRateLimiting(ApiHardeningExtensions.AuthRateLimitPolicy)]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>Authenticates with email + password, returning an access/refresh token pair.</summary>
    /// <param name="command">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Exchanges an expired access token + refresh token for a new pair (rotation).</summary>
    /// <param name="command">The current token pair.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Emails a 1-hour password reset token. Always returns 204 (no account enumeration).</summary>
    /// <param name="command">The account email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Completes a password reset using an emailed token.</summary>
    /// <param name="command">Email, token, and the new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Changes the authenticated user's password.</summary>
    /// <param name="command">Current and new passwords.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));
}
