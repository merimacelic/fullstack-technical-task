using ErrorOr;

namespace TaskManagement.Domain.Tasks;

public static class TaskErrors
{
    // Dynamic-arg errors carry their arguments in Metadata["args"] so the
    // API-edge ErrorLocalizer can string.Format them into the localised resx
    // template ({0}, {1}, ...). Description stays English for logs/ops.
    public static Error NotFound(TaskId id) =>
        Error.NotFound(
            "Task.NotFound",
            $"Task with id '{id}' was not found.",
            metadata: Args(id.ToString()));

    public static readonly Error TitleRequired =
        Error.Validation("Task.TitleRequired", "Task title must not be empty.");

    public static readonly Error OwnerRequired =
        Error.Validation("Task.OwnerRequired", "Task owner is required.");

    public static Error TitleTooLong(int maxLength) =>
        Error.Validation(
            "Task.TitleTooLong",
            $"Task title must not exceed {maxLength} characters.",
            metadata: Args(maxLength));

    public static Error DescriptionTooLong(int maxLength) =>
        Error.Validation(
            "Task.DescriptionTooLong",
            $"Task description must not exceed {maxLength} characters.",
            metadata: Args(maxLength));

    public static readonly Error DueDateInPast =
        Error.Validation("Task.DueDateInPast", "Due date cannot be in the past.");

    public static readonly Error AlreadyCompleted =
        Error.Conflict("Task.AlreadyCompleted", "Task is already completed.");

    public static readonly Error NotCompleted =
        Error.Conflict("Task.NotCompleted", "Task is not completed, cannot be reopened.");

    public static Error UnknownStatus(string name) =>
        Error.Validation(
            "Task.UnknownStatus",
            $"Unknown status '{name}'.",
            metadata: Args(name));

    public static Error UnknownPriority(string name) =>
        Error.Validation(
            "Task.UnknownPriority",
            $"Unknown priority '{name}'.",
            metadata: Args(name));

    private static Dictionary<string, object> Args(params object[] values) =>
        new() { ["args"] = values };
}
