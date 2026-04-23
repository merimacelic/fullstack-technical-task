using ErrorOr;

namespace TaskManagement.Domain.Tasks;

public static class TaskErrors
{
    public static Error NotFound(TaskId id) =>
        Error.NotFound("Task.NotFound", $"Task with id '{id}' was not found.");

    public static readonly Error TitleRequired =
        Error.Validation("Task.TitleRequired", "Task title must not be empty.");

    public static readonly Error OwnerRequired =
        Error.Validation("Task.OwnerRequired", "Task owner is required.");

    public static Error TitleTooLong(int maxLength) =>
        Error.Validation("Task.TitleTooLong", $"Task title must not exceed {maxLength} characters.");

    public static Error DescriptionTooLong(int maxLength) =>
        Error.Validation("Task.DescriptionTooLong", $"Task description must not exceed {maxLength} characters.");

    public static readonly Error DueDateInPast =
        Error.Validation("Task.DueDateInPast", "Due date cannot be in the past.");

    public static readonly Error AlreadyCompleted =
        Error.Conflict("Task.AlreadyCompleted", "Task is already completed.");

    public static readonly Error NotCompleted =
        Error.Conflict("Task.NotCompleted", "Task is not completed, cannot be reopened.");

    public static Error UnknownStatus(string name) =>
        Error.Validation("Task.UnknownStatus", $"Unknown status '{name}'.");

    public static Error UnknownPriority(string name) =>
        Error.Validation("Task.UnknownPriority", $"Unknown priority '{name}'.");
}
