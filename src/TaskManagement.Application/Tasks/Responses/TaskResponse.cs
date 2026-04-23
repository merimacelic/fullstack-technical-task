namespace TaskManagement.Application.Tasks.Responses;

/// <summary>Projected view of a <c>TaskItem</c> returned by the public API.</summary>
/// <param name="Id">Stable task identifier.</param>
/// <param name="Title">Short, human-readable task title.</param>
/// <param name="Description">Optional longer description; null when not provided.</param>
/// <param name="Status">Current lifecycle status: <c>Pending</c>, <c>InProgress</c>, or <c>Completed</c>.</param>
/// <param name="Priority">Priority label: <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
/// <param name="DueDateUtc">Optional due date in UTC; null if the task has no deadline.</param>
/// <param name="CreatedAtUtc">Creation timestamp in UTC.</param>
/// <param name="UpdatedAtUtc">Timestamp of the most recent mutation in UTC.</param>
/// <param name="CompletedAtUtc">Completion timestamp in UTC; null while the task is not completed.</param>
/// <param name="OrderKey">Decimal sort key for manual (drag-and-drop) ordering; lower values sort first.</param>
/// <param name="TagIds">Ids of tags associated with this task (empty when no tags are attached).</param>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    decimal OrderKey,
    IReadOnlyList<Guid> TagIds);
