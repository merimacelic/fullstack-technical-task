using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks;

// Named TaskItemStatus (not TaskStatus) to avoid clashing with System.Threading.Tasks.TaskStatus.
public sealed class TaskItemStatus : Enumeration<TaskItemStatus>
{
    public static readonly TaskItemStatus Pending = new(1, nameof(Pending));
    public static readonly TaskItemStatus InProgress = new(2, nameof(InProgress));
    public static readonly TaskItemStatus Completed = new(3, nameof(Completed));

    private TaskItemStatus(int id, string name)
        : base(id, name)
    {
    }

    public bool IsTerminal => this == Completed;
}
