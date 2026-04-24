using ErrorOr;
using Mediator;
using TaskManagement.Application.Users.Responses;

namespace TaskManagement.Application.Users.Commands.RefreshToken;

/// <summary>Request body for <c>POST /api/auth/refresh</c>.</summary>
/// <param name="RefreshToken">
/// The most recent refresh token returned to the client. Rotated on every successful call;
/// presenting a previously-rotated value revokes the whole token family and forces a new login.
/// </param>
public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<ErrorOr<AuthResponse>>;
