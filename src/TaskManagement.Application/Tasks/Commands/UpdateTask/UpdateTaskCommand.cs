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
public sealed record UpdateTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDateUtc,
    IReadOnlyList<Guid>? TagIds = null) : IRequest<ErrorOr<TaskResponse>>;
