using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;

namespace TaskManagement.Application.Users.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(_ => l["Auth.RefreshToken.Required"])
            .MaximumLength(512).WithMessage(_ => l["Auth.RefreshToken.TooLong"]);
    }
}
