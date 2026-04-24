namespace TaskManagement.Application.Common.Abstractions;

public sealed record IssuedTokens(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    string RefreshTokenHash,
    DateTime RefreshTokenExpiresUtc);

public interface IJwtTokenService
{
    IssuedTokens IssueFor(Guid userId, string email);

    string HashRefreshToken(string refreshToken);
}
