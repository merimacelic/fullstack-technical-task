using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.ChangeTaskPriority;

/// <summary>
/// Changes a task's priority without touching its other fields. Companion to
/// <see cref="ChangeTaskStatus.ChangeTaskStatusCommand"/> — use this when the
/// caller is flipping priority directly (e.g. a priority dropdown in the UI)
/// rather than editing the whole task.
/// </summary>
/// <param name="Id">Task id.</param>
/// <param name="Priority">Target priority name: <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
public sealed record ChangeTaskPriorityCommand(Guid Id, string Priority) : IRequest<ErrorOr<TaskResponse>>;
