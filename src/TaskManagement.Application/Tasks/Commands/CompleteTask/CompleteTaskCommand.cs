using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Commands.CompleteTask;

public sealed record CompleteTaskCommand(Guid Id) : IRequest<ErrorOr<TaskResponse>>;
