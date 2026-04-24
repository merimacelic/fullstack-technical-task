using TaskManagement.Domain.Users;

namespace TaskManagement.Domain.UnitTests.Users;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Issue_Should_CreateActive_Token_With_Expiry()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now, TimeSpan.FromDays(7));

        token.UserId.ShouldBe(UserId);
        token.TokenHash.ShouldBe("hash");
        token.CreatedAtUtc.ShouldBe(Now);
        token.ExpiresAtUtc.ShouldBe(Now.AddDays(7));
        token.RevokedAtUtc.ShouldBeNull();
        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_Should_BeFalse_AfterExpiry()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now, TimeSpan.FromMinutes(5));

        token.IsActive(Now.AddMinutes(6)).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_Should_MarkRevoked_And_StoreReplacement()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now, TimeSpan.FromDays(7));

        var result = token.Revoke(Now.AddHours(1), replacedByHash: "new-hash");

        result.IsError.ShouldBeFalse();
        token.RevokedAtUtc.ShouldBe(Now.AddHours(1));
        token.ReplacedByTokenHash.ShouldBe("new-hash");
        token.IsActive(Now.AddHours(1)).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_Should_Fail()
    {
        var token = RefreshToken.Issue(UserId, "hash", Now, TimeSpan.FromDays(7));
        token.Revoke(Now.AddHours(1)).IsError.ShouldBeFalse();

        var second = token.Revoke(Now.AddHours(2));

        second.IsError.ShouldBeTrue();
        second.FirstError.ShouldBe(UserErrors.InvalidRefreshToken);
    }
}
