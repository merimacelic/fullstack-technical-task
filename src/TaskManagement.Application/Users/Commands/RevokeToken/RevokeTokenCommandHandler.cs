using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;

namespace TaskManagement.Application.Users.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler
    : IRequestHandler<RevokeTokenCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public RevokeTokenCommandHandler(
        IApplicationDbContext dbContext,
        IJwtTokenService jwt,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _jwt = jwt;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(
        RevokeTokenCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new InvalidOperationException(
            "RevokeToken requires an authenticated user.");

        var presentedHash = _jwt.HashRefreshToken(request.RefreshToken);
        var now = _clock.UtcNow;

        // Idempotent + oracle-safe: unknown, expired, already-revoked, or belonging
        // to another user all collapse into the same silent-success response so a
        // caller can't probe token ownership. The ExecuteUpdate is scoped to the
        // acting user's own tokens so cross-user revoke is a no-op by construction.
        await _dbContext.RefreshTokens
            .Where(t => t.TokenHash == presentedHash
                        && t.UserId == userId
                        && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);

        return Result.Success;
    }
}
