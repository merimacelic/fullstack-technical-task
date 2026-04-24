using FluentValidation;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    // Hard cap so a client can't submit a multi-megabyte TagIds payload.
    public const int MaxTagsPerTask = 50;

    public CreateTaskCommandValidator(IStringLocalizer<SharedResource> l)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => l["Task.Title.Required"])
            .MaximumLength(TaskItem.MaxTitleLength).WithMessage(_ => l["Task.Title.TooLong"]);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength).WithMessage(_ => l["Task.Description.TooLong"])
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage(_ => l["Task.Priority.Required"])
            .Must(BeKnownPriority).WithMessage(_ => l["Task.Priority.Invalid"]);

        RuleFor(x => x.Status!)
            .Must(s => TaskItemStatus.TryFromName(s, out _))
            .WithMessage(_ => l["Task.Status.Invalid"])
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.TagIds!.Count)
            .LessThanOrEqualTo(MaxTagsPerTask).WithMessage(_ => l["Task.Tags.TooMany"])
            .When(x => x.TagIds is not null);
    }

    private static bool BeKnownPriority(string priority) =>
        TaskPriority.TryFromName(priority, out _);
}
