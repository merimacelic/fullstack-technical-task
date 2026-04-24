using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;

namespace TaskManagement.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => l["Auth.Email.Required"])
            .EmailAddress().WithMessage(_ => l["Auth.Email.Invalid"])
            .MaximumLength(256).WithMessage(_ => l["Auth.Email.TooLong"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => l["Auth.Password.Required"])
            .MinimumLength(8).WithMessage(_ => l["Auth.Password.TooShort"])
            .MaximumLength(128).WithMessage(_ => l["Auth.Password.TooLong"])
            .Matches("[A-Z]").WithMessage(_ => l["Auth.Password.Upper"])
            .Matches("[a-z]").WithMessage(_ => l["Auth.Password.Lower"])
            .Matches("[0-9]").WithMessage(_ => l["Auth.Password.Digit"]);
    }
}
