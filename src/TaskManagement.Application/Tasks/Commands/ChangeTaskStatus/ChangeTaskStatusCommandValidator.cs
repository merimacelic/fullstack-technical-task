using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => l["Task.Id.Required"]);

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(_ => l["Task.Status.Invalid"])
            .Must(s => TaskItemStatus.TryFromName(s, out _))
            .WithMessage(_ => l["Task.Status.Invalid"]);
    }
}
