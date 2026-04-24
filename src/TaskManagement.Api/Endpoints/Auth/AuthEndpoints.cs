using Mediator;
using TaskManagement.Api.Infrastructure;
using TaskManagement.Application.Users.Commands.LoginUser;
using TaskManagement.Application.Users.Commands.RefreshToken;
using TaskManagement.Application.Users.Commands.RegisterUser;
using TaskManagement.Application.Users.Commands.RevokeToken;

namespace TaskManagement.Api.Endpoints.Auth;

public sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Every auth endpoint sits behind the stricter "auth" token-bucket limiter
        // so password brute-force and token replay can't hide in the wider 100/min
        // global bucket. Register covers account enumeration; login/refresh cover
        // credential stuffing; revoke is authenticated but kept here for symmetry.
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth")
            .WithOpenApi();

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .WithName("RegisterUser")
            .WithSummary("Register a new user and return an access + refresh token pair.");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("LoginUser")
            .WithSummary("Authenticate with email + password and return an access + refresh token pair.");

        group.MapPost("/refresh", Refresh)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithSummary("Exchange a valid refresh token for a new access + refresh token pair.");

        group.MapPost("/revoke", Revoke)
            .RequireAuthorization()
            .WithName("RevokeToken")
            .WithSummary("Revoke a refresh token so it cannot be used again (logout).");
    }

    private static async Task<IResult> Register(
        RegisterUserCommand command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> Login(
        LoginUserCommand command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> Refresh(
        RefreshTokenCommand command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> Revoke(
        RevokeTokenCommand command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToNoContent(http);
    }
}
