using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;

namespace TaskManagement.Application.Users.Commands.RevokeToken;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(_ => l["Auth.RefreshToken.Required"])
            .MaximumLength(512).WithMessage(_ => l["Auth.RefreshToken.TooLong"]);
    }
}
