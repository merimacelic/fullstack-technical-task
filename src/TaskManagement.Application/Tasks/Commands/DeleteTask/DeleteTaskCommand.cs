using ErrorOr;
using Mediator;

namespace TaskManagement.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
