using ErrorOr;
using Mediator;
using TaskManagement.Application.Users.Responses;

namespace TaskManagement.Application.Users.Commands.RegisterUser;

/// <summary>Request body for <c>POST /api/auth/register</c>.</summary>
/// <param name="Email">Email address used as the login handle. Must be a valid address.</param>
/// <param name="Password">
/// Password. Must be at least 8 characters and include an uppercase letter, a lowercase letter,
/// and a digit. Returns a deliberately generic validation error on failure (both weak passwords
/// and duplicate emails collapse into the same response to avoid enumeration).
/// </param>
public sealed record RegisterUserCommand(string Email, string Password)
    : IRequest<ErrorOr<AuthResponse>>;
