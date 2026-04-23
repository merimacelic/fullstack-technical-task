using FluentValidation;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    // Hard cap so a client can't submit a multi-megabyte TagIds payload.
    public const int MaxTagsPerTask = 50;

    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(TaskItem.MaxTitleLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(BeKnownPriority)
            .WithMessage("Priority must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.TagIds!.Count)
            .LessThanOrEqualTo(MaxTagsPerTask)
            .WithMessage($"A task can be associated with at most {MaxTagsPerTask} tags.")
            .When(x => x.TagIds is not null);
    }

    private static bool BeKnownPriority(string priority) =>
        TaskPriority.TryFromName(priority, out _);
}
