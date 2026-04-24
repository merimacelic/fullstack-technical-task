using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Tags.Responses;

namespace TaskManagement.Application.Tasks.Tags.Commands.CreateTag;

/// <summary>Request body for <c>POST /api/tags</c> — creates a tag scoped to the caller.</summary>
/// <param name="Name">Tag label. Required, 1–50 characters. Must be unique per owner.</param>
public sealed record CreateTagCommand(string Name) : IRequest<ErrorOr<TagResponse>>;
