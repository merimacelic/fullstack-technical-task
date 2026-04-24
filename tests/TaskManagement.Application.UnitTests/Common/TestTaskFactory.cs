using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Common;

// Test-only factory: TaskItem.Create now requires an explicit OrderKey (which handlers
// compute via OrderKeyService). Handler-level tests don't care about the key, so
// funnel creation through this helper with a simple counter-based default.
internal static class TestTaskFactory
{
    private static int _counter;

    public static TaskItem New(
        Guid ownerId,
        string title = "t",
        TaskPriority? priority = null,
        DateTime? dueDateUtc = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var orderKey = (Interlocked.Increment(ref _counter) * TaskItem.OrderKeyStep) + TaskItem.OrderKeyStep;
        return TaskItem.Create(
            ownerId,
            title,
            description: null,
            priority ?? TaskPriority.Low,
            dueDateUtc,
            now,
            orderKey).Value;
    }
}
