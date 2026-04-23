using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.ReopenTask;

public sealed record ReopenTaskCommand(Guid Id) : IRequest<ErrorOr<TaskResponse>>;
