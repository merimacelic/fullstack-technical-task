namespace TaskManagement.Domain.Tasks;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(TaskId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TaskId id, CancellationToken cancellationToken = default);

    void Add(TaskItem task);

    void Remove(TaskItem task);
}
