namespace TaskManagement.Application.Users.Responses;

/// <summary>
/// Token pair returned by <c>POST /api/auth/register</c>, <c>/login</c>, and <c>/refresh</c>.
/// Clients send the access token as a <c>Bearer</c> header on subsequent calls and exchange
/// the refresh token for a new pair before the access token expires.
/// </summary>
/// <param name="UserId">Id of the authenticated user.</param>
/// <param name="Email">Email address the user authenticated with.</param>
/// <param name="AccessToken">Short-lived JWT; attach as <c>Authorization: Bearer ...</c>.</param>
/// <param name="AccessTokenExpiresUtc">UTC timestamp after which the access token will be rejected.</param>
/// <param name="RefreshToken">
/// Opaque, single-use token used to obtain a new pair. Rotated on every refresh; replaying a
/// revoked value triggers a family revocation and forces the user to sign in again.
/// </param>
/// <param name="RefreshTokenExpiresUtc">UTC timestamp after which the refresh token will be rejected.</param>
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc);
