using Mediator;
using TaskManagement.Api.Infrastructure;
using TaskManagement.Application.Tasks.Tags.Commands.CreateTag;
using TaskManagement.Application.Tasks.Tags.Commands.DeleteTag;
using TaskManagement.Application.Tasks.Tags.Commands.RenameTag;
using TaskManagement.Application.Tasks.Tags.Queries.GetTags;

namespace TaskManagement.Api.Endpoints.Tags;

public sealed class TagEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tags")
            .WithTags("Tags")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetTags)
            .WithName("GetTags")
            .WithSummary("List the current user's tags with per-tag task counts.");

        group.MapPost("/", CreateTag)
            .WithName("CreateTag")
            .WithSummary("Create a new tag scoped to the current user.");

        group.MapPut("/{id:guid}", RenameTag)
            .WithName("RenameTag")
            .WithSummary("Rename an existing tag.");

        group.MapDelete("/{id:guid}", DeleteTag)
            .WithName("DeleteTag")
            .WithSummary("Delete a tag and remove it from every associated task.");
    }

    private static async Task<IResult> GetTags(
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTagsQuery(), cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> CreateTag(
        CreateTagCommand command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreated(http, value => $"/api/tags/{value.Id}");
    }

    private static async Task<IResult> RenameTag(
        Guid id,
        RenameTagBody body,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameTagCommand(id, body.Name), cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> DeleteTag(
        Guid id,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTagCommand(id), cancellationToken);
        return result.ToNoContent(http);
    }

    /// <summary>Request body for <c>PUT /api/tags/{id}</c>.</summary>
    /// <param name="Name">New tag label. Required, 1–50 characters. Must be unique per owner.</param>
    public sealed record RenameTagBody(string Name);
}
