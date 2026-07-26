using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Auth.Login;

/// <summary>Authenticates a user with email + password (FR-AUTH-001).</summary>
/// <param name="Email">Login email.</param>
/// <param name="Password">Plain-text password (verified against the stored hash).</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
