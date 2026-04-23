using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks;

public sealed class TaskPriority : Enumeration<TaskPriority>
{
    public static readonly TaskPriority Low = new(1, nameof(Low));
    public static readonly TaskPriority Medium = new(2, nameof(Medium));
    public static readonly TaskPriority High = new(3, nameof(High));
    public static readonly TaskPriority Critical = new(4, nameof(Critical));

    private TaskPriority(int id, string name)
        : base(id, name)
    {
    }
}
