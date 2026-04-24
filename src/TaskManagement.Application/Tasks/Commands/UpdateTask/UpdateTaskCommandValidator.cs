using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage(_ => l["Task.Id.Required"]);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => l["Task.Title.Required"])
            .MaximumLength(TaskItem.MaxTitleLength).WithMessage(_ => l["Task.Title.TooLong"]);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength).WithMessage(_ => l["Task.Description.TooLong"])
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage(_ => l["Task.Priority.Required"])
            .Must(p => TaskPriority.TryFromName(p, out _))
            .WithMessage(_ => l["Task.Priority.Invalid"]);

        RuleFor(x => x.Status!)
            .Must(s => TaskItemStatus.TryFromName(s, out _))
            .WithMessage(_ => l["Task.Status.Invalid"])
            .When(x => !string.IsNullOrEmpty(x.Status));

        // TagIds: null = leave tags untouched, [] = clear all tags, non-empty = replace.
        // Capped so clients can't submit a multi-megabyte payload.
        RuleFor(x => x.TagIds!.Count)
            .LessThanOrEqualTo(CreateTaskCommandValidator.MaxTagsPerTask)
            .WithMessage(_ => l["Task.Tags.TooMany"])
            .When(x => x.TagIds is not null);
    }
}
