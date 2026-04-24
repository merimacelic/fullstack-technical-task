using ErrorOr;
using Mediator;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Users.Responses;
using TaskManagement.Domain.Users;
using DomainRefreshToken = TaskManagement.Domain.Users.RefreshToken;

namespace TaskManagement.Application.Users.Commands.LoginUser;

public sealed class LoginUserCommandHandler
    : IRequestHandler<LoginUserCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwt;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    public LoginUserCommandHandler(
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
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var validated = await _userService.ValidateCredentialsAsync(
            request.Email, request.Password, cancellationToken);
        if (validated.IsError)
        {
            return validated.Errors;
        }

        var user = validated.Value;
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
