using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags.Commands.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => l["Tag.Name.Required"])
            .MaximumLength(Tag.MaxNameLength).WithMessage(_ => l["Tag.Name.TooLong"]);
    }
}
