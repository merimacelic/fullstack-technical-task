using FluentValidation;

namespace TaskManagement.Application.Tasks.Commands.ReorderTask;

public sealed class ReorderTaskCommandValidator : AbstractValidator<ReorderTaskCommand>
{
    public ReorderTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.PreviousTaskId)
            .Must((cmd, previous) => previous != cmd.Id)
            .WithMessage("A task cannot be its own previous neighbour.");

        RuleFor(x => x.NextTaskId)
            .Must((cmd, next) => next != cmd.Id)
            .WithMessage("A task cannot be its own next neighbour.");

        RuleFor(x => x)
            .Must(x => x.PreviousTaskId is null || x.NextTaskId is null || x.PreviousTaskId != x.NextTaskId)
            .WithMessage("Previous and next neighbours must be different tasks.");
    }
}
