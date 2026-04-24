using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;

namespace TaskManagement.Application.Users.Commands.LoginUser;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => l["Auth.Email.Required"])
            .EmailAddress().WithMessage(_ => l["Auth.Email.Invalid"])
            .MaximumLength(256).WithMessage(_ => l["Auth.Email.TooLong"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => l["Auth.Password.Required"])
            .MaximumLength(128).WithMessage(_ => l["Auth.Password.TooLong"]);
    }
}
