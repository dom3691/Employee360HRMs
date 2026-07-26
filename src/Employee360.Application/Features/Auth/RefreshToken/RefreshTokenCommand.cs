using Employee360.Application.Features.Auth.Login;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Auth.RefreshToken;

/// <summary>
/// Exchanges an expired access token + active refresh token for a new pair
/// (single-use rotation, FR-AUTH-001).
/// </summary>
/// <param name="AccessToken">The expired (or expiring) JWT.</param>
/// <param name="RefreshToken">The opaque refresh token issued alongside it.</param>
public sealed record RefreshTokenCommand(string AccessToken, string RefreshToken)
    : IRequest<Result<LoginResponse>>;
