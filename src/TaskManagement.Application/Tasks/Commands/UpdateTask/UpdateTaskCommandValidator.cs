using FluentValidation;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(TaskItem.MaxTitleLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(p => TaskPriority.TryFromName(p, out _))
            .WithMessage("Priority must be one of: Low, Medium, High, Critical.");

        // TagIds: null = leave tags untouched, [] = clear all tags, non-empty = replace.
        // Capped so clients can't submit a multi-megabyte payload.
        RuleFor(x => x.TagIds!.Count)
            .LessThanOrEqualTo(CreateTaskCommandValidator.MaxTagsPerTask)
            .WithMessage($"A task can be associated with at most {CreateTaskCommandValidator.MaxTagsPerTask} tags.")
            .When(x => x.TagIds is not null);
    }
}
