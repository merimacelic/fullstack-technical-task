using ErrorOr;

namespace TaskManagement.Application.Common.Abstractions;

public sealed record AuthenticatedUser(Guid Id, string Email);

public interface IUserService
{
    Task<ErrorOr<AuthenticatedUser>> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthenticatedUser>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthenticatedUser>> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
