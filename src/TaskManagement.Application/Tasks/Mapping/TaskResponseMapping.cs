using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Mapping;

internal static class TaskResponseMapping
{
    public static TaskResponse ToResponse(this TaskItem task) =>
        new(
            task.Id.Value,
            task.Title,
            task.Description,
            task.Status.Name,
            task.Priority.Name,
            task.DueDateUtc,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.CompletedAtUtc,
            task.OrderKey,
            task.TagIds.Select(t => t.Value).ToList());
}
