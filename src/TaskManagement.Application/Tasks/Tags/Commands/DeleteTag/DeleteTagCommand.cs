using ErrorOr;
using Mediator;

namespace TaskManagement.Application.Tasks.Tags.Commands.DeleteTag;

public sealed record DeleteTagCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
