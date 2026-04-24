using ErrorOr;
using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Users;

public sealed class RefreshToken : AggregateRoot<RefreshTokenId>
{
    private RefreshToken(
        RefreshTokenId id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    // EF Core materialization constructor.
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public Guid UserId { get; private set; }

    // Every refresh token issued from the same login shares a FamilyId. On replay
    // detection (a revoked token being presented again), every active member of
    // the family is revoked — blowing up the whole session, not just one token.
    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    // New login/register: omit familyId to start a fresh family. Rotation: pass the
    // parent's FamilyId so the chain stays linkable for replay detection.
    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTime nowUtc,
        TimeSpan lifetime,
        Guid? familyId = null) =>
        new(
            RefreshTokenId.New(),
            userId,
            familyId ?? Guid.NewGuid(),
            tokenHash,
            nowUtc.Add(lifetime),
            nowUtc);

    public bool IsActive(DateTime nowUtc) =>
        RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    public ErrorOr<Success> Revoke(DateTime nowUtc, string? replacedByHash = null)
    {
        if (RevokedAtUtc is not null)
        {
            return UserErrors.InvalidRefreshToken;
        }

        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacedByHash;
        return Result.Success;
    }
}
