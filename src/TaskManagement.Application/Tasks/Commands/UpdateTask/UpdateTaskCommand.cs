using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.UpdateTask;

/// <summary>CQRS command dispatched by <c>PUT /api/tasks/{id}</c>.</summary>
/// <param name="Id">Task id (from the route).</param>
/// <param name="Title">Required, 1–200 characters.</param>
/// <param name="Description">Optional, up to 2000 characters.</param>
/// <param name="Priority">One of <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
/// <param name="DueDateUtc">Optional due date in UTC.</param>
/// <param name="TagIds">
/// <c>null</c> leaves existing tags untouched; an empty array clears all tags; a non-empty
/// array replaces the tag set. Every id must belong to the caller. Capped at 50 per task.
/// </param>
/// <param name="Status">
/// Optional target status. <c>null</c> leaves the current status untouched. When provided,
/// the task is transitioned via the same domain rules as the dedicated /status endpoint —
/// <c>CompletedAtUtc</c> is reset on any move off <c>Completed</c>.
/// </param>
public sealed record UpdateTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDateUtc,
    IReadOnlyList<Guid>? TagIds = null,
    string? Status = null) : IRequest<ErrorOr<TaskResponse>>;
