using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// Transitions a task to any valid status. Complement to the action-oriented
/// <c>Complete</c>/<c>Reopen</c> commands — use those for their specific intent,
/// use this when the caller is picking a target state directly (e.g. a status
/// dropdown in the UI).
/// </summary>
/// <param name="Id">Task id.</param>
/// <param name="Status">Target status name: <c>Pending</c>, <c>InProgress</c>, or <c>Completed</c>.</param>
public sealed record ChangeTaskStatusCommand(Guid Id, string Status) : IRequest<ErrorOr<TaskResponse>>;
