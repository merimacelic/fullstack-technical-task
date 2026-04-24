using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Users.Responses;
using TaskManagement.Domain.Users;
using DomainRefreshToken = TaskManagement.Domain.Users.RefreshToken;

namespace TaskManagement.Application.Users.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwt;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    public RefreshTokenCommandHandler(
        IUserService userService,
        IJwtTokenService jwt,
        IApplicationDbContext dbContext,
        IDateTimeProvider clock)
    {
        _userService = userService;
        _jwt = jwt;
        _dbContext = dbContext;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var presentedHash = _jwt.HashRefreshToken(request.RefreshToken);
        var now = _clock.UtcNow;

        var stored = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == presentedHash, cancellationToken);

        if (stored is null || now >= stored.ExpiresAtUtc)
        {
            return UserErrors.InvalidRefreshToken;
        }

        // Replay detection — re-presenting a revoked token means the chain is
        // compromised (attacker captured an old token, or a client double-submitted).
        // Revoke every active descendant in the family and reject the call.
        if (stored.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return UserErrors.InvalidRefreshToken;
        }

        var userResult = await _userService.FindByIdAsync(stored.UserId, cancellationToken);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        var user = userResult.Value;
        var tokens = _jwt.IssueFor(user.Id, user.Email);

        // Atomic revoke: row only flips from active → revoked if nobody else beat
        // us to it. If rows-affected is 0, a concurrent call consumed the same
        // refresh token first — treat it as replay and kill the family.
        var rowsAffected = await _dbContext.RefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setter => setter
                    .SetProperty(t => t.RevokedAtUtc, now)
                    .SetProperty(t => t.ReplacedByTokenHash, tokens.RefreshTokenHash),
                cancellationToken);

        if (rowsAffected == 0)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return UserErrors.InvalidRefreshToken;
        }

        var newToken = DomainRefreshToken.Issue(
            user.Id,
            tokens.RefreshTokenHash,
            now,
            tokens.RefreshTokenExpiresUtc - now,
            stored.FamilyId);

        _dbContext.RefreshTokens.Add(newToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email,
            tokens.AccessToken,
            tokens.AccessTokenExpiresUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresUtc);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken ct) =>
        _dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setter => setter.SetProperty(t => t.RevokedAtUtc, now), ct);
}
