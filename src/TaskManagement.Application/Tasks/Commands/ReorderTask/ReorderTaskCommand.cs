using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.ReorderTask;

/// <summary>CQRS command dispatched by <c>PATCH /api/tasks/{id}/reorder</c>.</summary>
/// <param name="Id">Id of the task being moved (from the route).</param>
/// <param name="PreviousTaskId">
/// Id of the task that should end up immediately above the moved one, or <c>null</c> to drop it
/// at the top of the list.
/// </param>
/// <param name="NextTaskId">
/// Id of the task that should end up immediately below the moved one, or <c>null</c> to drop it
/// at the bottom of the list.
/// </param>
public sealed record ReorderTaskCommand(
    Guid Id,
    Guid? PreviousTaskId,
    Guid? NextTaskId) : IRequest<ErrorOr<TaskResponse>>;
