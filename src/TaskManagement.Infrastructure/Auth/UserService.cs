using ErrorOr;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Domain.Users;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Auth;

internal sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ErrorOr<AuthenticatedUser>> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Existence check is skipped on purpose: letting the unique-email constraint
        // surface on CreateAsync (with its generic "DuplicateUserName" failure) keeps
        // the response identical to a password-policy rejection, so /api/auth/register
        // can't be used as a user-enumeration oracle. The auth rate limiter mops up
        // the rest of the brute-force surface.
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return UserErrors.RegistrationFailed;
        }

        return new AuthenticatedUser(user.Id, user.Email!);
    }

    public async Task<ErrorOr<AuthenticatedUser>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return UserErrors.InvalidCredentials;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return UserErrors.LockedOut;
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                return UserErrors.LockedOut;
            }

            return UserErrors.InvalidCredentials;
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return new AuthenticatedUser(user.Id, user.Email!);
    }

    public async Task<ErrorOr<AuthenticatedUser>> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserErrors.InvalidCredentials;
        }

        return new AuthenticatedUser(user.Id, user.Email!);
    }
}
