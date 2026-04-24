using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.ChangeTaskPriority;

public sealed class ChangeTaskPriorityCommandValidator : AbstractValidator<ChangeTaskPriorityCommand>
{
    public ChangeTaskPriorityCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => l["Task.Id.Required"]);

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage(_ => l["Task.Priority.Required"])
            .Must(p => TaskPriority.TryFromName(p, out _))
            .WithMessage(_ => l["Task.Priority.Invalid"]);
    }
}
