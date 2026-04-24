using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Api.IntegrationTests.Infrastructure;
using TaskManagement.Application.Users.Responses;

namespace TaskManagement.Api.IntegrationTests.Auth;

public class AuthEndpointsTests : IntegrationTestBase
{
    public AuthEndpointsTests(TaskManagementApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Register_Should_Issue_AccessAndRefreshTokens()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"user-{Guid.NewGuid():N}@icon.test",
            password = "Passw0rd!",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync<AuthResponse>(response);
        body.AccessToken.ShouldNotBeNullOrWhiteSpace();
        body.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        body.AccessTokenExpiresUtc.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_Should_Reject_WeakPassword()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "weak@icon.test",
            password = "short",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Should_Reject_DuplicateEmail_With_Generic_ValidationError()
    {
        var email = $"dup-{Guid.NewGuid():N}@icon.test";
        await Client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });

        var second = await Client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });

        // Deliberately generic: duplicate-email returns the same 400 as password-policy
        // failures so the endpoint can't be used as a user-enumeration oracle.
        second.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await second.Content.ReadAsStringAsync();
        body.ShouldNotContain(email, Case.Insensitive);
        body.ShouldNotContain("already", Case.Insensitive);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Should_Return_Tokens()
    {
        var email = $"login-{Guid.NewGuid():N}@icon.test";
        await Client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = "Passw0rd!" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync<AuthResponse>(response);
        body.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithBadPassword_Should_Return_401()
    {
        var email = $"bad-{Guid.NewGuid():N}@icon.test";
        await Client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPass1!",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Should_Rotate_Tokens_And_Invalidate_OldRefresh()
    {
        var first = await RegisterAsync();

        var refreshResp = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        refreshResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var next = await ReadJsonAsync<AuthResponse>(refreshResp);
        next.RefreshToken.ShouldNotBe(first.RefreshToken);

        var reuseResp = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        reuseResp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Replay_Should_Revoke_Entire_TokenFamily()
    {
        // Classic stolen-refresh-token scenario: attacker calls refresh with an
        // already-rotated token. The handler must detect the replay, blow up the
        // whole family (the legitimate client's current token included), and force
        // everyone back to login. This is the behaviour documented in the
        // IETF OAuth 2.0 Security Best Current Practice for refresh-token rotation.
        var first = await RegisterAsync();

        var rotated = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        rotated.StatusCode.ShouldBe(HttpStatusCode.OK);
        var next = await ReadJsonAsync<AuthResponse>(rotated);

        // Replay the original — expected: 401 AND the freshly-issued token is now dead too.
        var replay = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var postReplayRefresh = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = next.RefreshToken });
        postReplayRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_UnknownToken_Should_ReturnNoContent_Silently()
    {
        // Oracle-safety: revoking a token that doesn't exist (or belongs to someone
        // else) must return the same shape as revoking your own token. Otherwise
        // the status-code delta leaks token ownership/existence.
        var auth = await RegisterAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var revoke = await Client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = "not-a-real-refresh-token-" + Guid.NewGuid().ToString("N"),
        });

        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_Should_InvalidateRefreshToken()
    {
        var auth = await RegisterAsync();

        using var revokeClient = CreateUnauthenticatedClient();
        revokeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var revoke = await revokeClient.PostAsJsonAsync("/api/auth/revoke", new { refreshToken = auth.RefreshToken });
        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var reuse = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
