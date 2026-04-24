using FluentValidation;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags.Commands.RenameTag;

public sealed class RenameTagCommandValidator : AbstractValidator<RenameTagCommand>
{
    public RenameTagCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Tag.MaxNameLength);
    }
}
