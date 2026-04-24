using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Infrastructure;
using TaskManagement.Application.Tasks.Commands.ChangeTaskPriority;
using TaskManagement.Application.Tasks.Commands.ChangeTaskStatus;
using TaskManagement.Application.Tasks.Commands.CompleteTask;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Application.Tasks.Commands.DeleteTask;
using TaskManagement.Application.Tasks.Commands.ReopenTask;
using TaskManagement.Application.Tasks.Commands.ReorderTask;
using TaskManagement.Application.Tasks.Commands.UpdateTask;
using TaskManagement.Application.Tasks.Queries.GetTaskById;
using TaskManagement.Application.Tasks.Queries.GetTasks;

namespace TaskManagement.Api.Endpoints.Tasks;

public sealed class TaskEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetTasks)
            .WithName("GetTasks")
            .WithSummary("List tasks with optional status/priority filter, search, sort, and pagination.");

        group.MapGet("/{id:guid}", GetTaskById)
            .WithName("GetTaskById")
            .WithSummary("Fetch a single task by its id.");

        group.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .WithSummary("Create a new task, optionally tagged.")
            .WithDescription("TagIds is optional; when provided, every id must belong to the caller. Capped at 50 tags.");

        group.MapPut("/{id:guid}", UpdateTask)
            .WithName("UpdateTask")
            .WithSummary("Update a task's title, description, priority, due date, and optionally its tags.")
            .WithDescription(
                "TagIds semantics: omit (null) to leave existing tags untouched, " +
                "send an empty array [] to clear all tags, or send a non-empty array " +
                "to replace the full tag set. Capped at 50 tags per task.");

        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask")
            .WithSummary("Delete a task permanently.");

        group.MapPatch("/{id:guid}/complete", CompleteTask)
            .WithName("CompleteTask")
            .WithSummary("Mark a task as completed.");

        group.MapPatch("/{id:guid}/reopen", ReopenTask)
            .WithName("ReopenTask")
            .WithSummary("Reopen a completed task back to Pending.");

        group.MapPatch("/{id:guid}/status", ChangeStatus)
            .WithName("ChangeTaskStatus")
            .WithSummary("Transition a task to a chosen status (Pending, InProgress, or Completed).");

        group.MapPatch("/{id:guid}/priority", ChangePriority)
            .WithName("ChangeTaskPriority")
            .WithSummary("Change a task's priority (Low, Medium, High, or Critical).");

        group.MapPatch("/{id:guid}/reorder", ReorderTask)
            .WithName("ReorderTask")
            .WithSummary("Move a task between two neighbours (drag-and-drop).");
    }

    private static async Task<IResult> GetTasks(
        [AsParameters] GetTasksQuery query,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> GetTaskById(
        Guid id,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTaskByIdQuery(id), cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskCommand command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreated(http, value => $"/api/tasks/{value.Id}");
    }

    private static async Task<IResult> UpdateTask(
        Guid id,
        UpdateTaskBody body,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskCommand(
            id,
            body.Title,
            body.Description,
            body.Priority,
            body.DueDateUtc,
            body.TagIds,
            body.Status);
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> ReorderTask(
        Guid id,
        ReorderTaskBody body,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var command = new ReorderTaskCommand(id, body.PreviousTaskId, body.NextTaskId);
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> DeleteTask(
        Guid id,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTaskCommand(id), cancellationToken);
        return result.ToNoContent(http);
    }

    private static async Task<IResult> CompleteTask(
        Guid id,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteTaskCommand(id), cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> ReopenTask(
        Guid id,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReopenTaskCommand(id), cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> ChangeStatus(
        Guid id,
        ChangeTaskStatusBody body,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeTaskStatusCommand(id, body.Status), cancellationToken);
        return result.ToOk(http);
    }

    private static async Task<IResult> ChangePriority(
        Guid id,
        ChangeTaskPriorityBody body,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeTaskPriorityCommand(id, body.Priority), cancellationToken);
        return result.ToOk(http);
    }

    /// <summary>Request body for <c>PUT /api/tasks/{id}</c>.</summary>
    /// <param name="Title">Required, 1–200 characters.</param>
    /// <param name="Description">Optional, up to 2000 characters.</param>
    /// <param name="Priority"><c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
    /// <param name="DueDateUtc">Optional due date in UTC.</param>
    /// <param name="TagIds">
    /// <c>null</c> leaves existing tags untouched; an empty array clears all tags; a non-empty
    /// array replaces the tag set. Every id must belong to the caller. Capped at 50 per task.
    /// </param>
    /// <param name="Status">
    /// Optional target status. <c>null</c> leaves the current status untouched. Allowed values:
    /// <c>Pending</c>, <c>InProgress</c>, <c>Completed</c>.
    /// </param>
    public sealed record UpdateTaskBody(
        string Title,
        string? Description,
        string Priority,
        DateTime? DueDateUtc,
        IReadOnlyList<Guid>? TagIds = null,
        string? Status = null);

    /// <summary>Request body for <c>PATCH /api/tasks/{id}/reorder</c> (drag-and-drop).</summary>
    /// <param name="PreviousTaskId">Task to land above the moved one, or <c>null</c> for top of list.</param>
    /// <param name="NextTaskId">Task to land below the moved one, or <c>null</c> for bottom of list.</param>
    public sealed record ReorderTaskBody(Guid? PreviousTaskId, Guid? NextTaskId);

    /// <summary>Request body for <c>PATCH /api/tasks/{id}/status</c>.</summary>
    /// <param name="Status">Target status name: <c>Pending</c>, <c>InProgress</c>, or <c>Completed</c>.</param>
    public sealed record ChangeTaskStatusBody(string Status);

    /// <summary>Request body for <c>PATCH /api/tasks/{id}/priority</c>.</summary>
    /// <param name="Priority">Target priority name: <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
    public sealed record ChangeTaskPriorityBody(string Priority);
}
