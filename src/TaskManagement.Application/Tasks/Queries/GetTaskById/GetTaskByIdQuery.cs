using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(Guid Id) : IRequest<ErrorOr<TaskResponse>>;
