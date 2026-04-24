using ErrorOr;
using Mediator;
using TaskManagement.Application.Users.Responses;

namespace TaskManagement.Application.Users.Commands.LoginUser;

/// <summary>Request body for <c>POST /api/auth/login</c>.</summary>
/// <param name="Email">Email address of an existing account.</param>
/// <param name="Password">Account password. Five consecutive failures lock the account for 5 minutes.</param>
public sealed record LoginUserCommand(string Email, string Password)
    : IRequest<ErrorOr<AuthResponse>>;
