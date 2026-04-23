using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.CreateTask;

/// <summary>Request body for <c>POST /api/tasks</c> — creates a task owned by the caller.</summary>
/// <param name="Title">Required, 1–200 characters.</param>
/// <param name="Description">Optional, up to 2000 characters.</param>
/// <param name="Priority">One of <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
/// <param name="DueDateUtc">Optional due date in UTC; may not be before today.</param>
/// <param name="TagIds">
/// Optional ids of tags to attach. Every id must belong to the caller. Capped at 50 per task.
/// </param>
public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDateUtc,
    IReadOnlyList<Guid>? TagIds = null) : IRequest<ErrorOr<TaskResponse>>;
