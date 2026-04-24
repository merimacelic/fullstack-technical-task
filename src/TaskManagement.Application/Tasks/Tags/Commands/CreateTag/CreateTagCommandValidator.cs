using FluentValidation;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags.Commands.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Tag.MaxNameLength);
    }
}
