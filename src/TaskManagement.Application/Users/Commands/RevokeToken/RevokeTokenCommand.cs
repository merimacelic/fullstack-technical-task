using ErrorOr;
using Mediator;

namespace TaskManagement.Application.Users.Commands.RevokeToken;

/// <summary>Request body for <c>POST /api/auth/revoke</c> (logout).</summary>
/// <param name="RefreshToken">
/// Refresh token to revoke. Always returns 204, regardless of whether the token existed,
/// was expired, or belonged to a different user, so the endpoint can't be used to probe
/// token ownership.
/// </param>
public sealed record RevokeTokenCommand(string RefreshToken)
    : IRequest<ErrorOr<Success>>;
