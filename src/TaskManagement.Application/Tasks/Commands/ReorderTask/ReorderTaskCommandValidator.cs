using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;

namespace TaskManagement.Application.Tasks.Commands.ReorderTask;

public sealed class ReorderTaskCommandValidator : AbstractValidator<ReorderTaskCommand>
{
    public ReorderTaskCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => l["Task.Id.Required"]);

        RuleFor(x => x.PreviousTaskId)
            .Must((cmd, previous) => previous != cmd.Id)
            .WithMessage(_ => l["Task.Reorder.SelfPrev"]);

        RuleFor(x => x.NextTaskId)
            .Must((cmd, next) => next != cmd.Id)
            .WithMessage(_ => l["Task.Reorder.SelfNext"]);

        RuleFor(x => x)
            .Must(x => x.PreviousTaskId is null || x.NextTaskId is null || x.PreviousTaskId != x.NextTaskId)
            .WithMessage(_ => l["Task.Reorder.SameNeighbour"]);
    }
}
