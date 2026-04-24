using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Tags.Responses;

namespace TaskManagement.Application.Tasks.Tags.Commands.RenameTag;

public sealed record RenameTagCommand(Guid Id, string Name) : IRequest<ErrorOr<TagResponse>>;
