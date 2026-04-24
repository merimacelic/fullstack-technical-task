using ErrorOr;
using Mediator;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Users.Responses;
using TaskManagement.Domain.Users;
using DomainRefreshToken = TaskManagement.Domain.Users.RefreshToken;

namespace TaskManagement.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwt;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    public RegisterUserCommandHandler(
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
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var registered = await _userService.RegisterAsync(request.Email, request.Password, cancellationToken);
        if (registered.IsError)
        {
            return registered.Errors;
        }

        var user = registered.Value;
        var tokens = _jwt.IssueFor(user.Id, user.Email);
        var refreshToken = DomainRefreshToken.Issue(
            user.Id,
            tokens.RefreshTokenHash,
            _clock.UtcNow,
            tokens.RefreshTokenExpiresUtc - _clock.UtcNow);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email,
            tokens.AccessToken,
            tokens.AccessTokenExpiresUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresUtc);
    }
}
