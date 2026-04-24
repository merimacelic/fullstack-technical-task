using ErrorOr;
using Mediator;
using TaskManagement.Application.Tasks.Tags.Responses;

namespace TaskManagement.Application.Tasks.Tags.Queries.GetTags;

public sealed record GetTagsQuery : IRequest<ErrorOr<IReadOnlyList<TagResponse>>>;
