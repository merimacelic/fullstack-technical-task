using ErrorOr;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks.Events;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Domain.Tasks;

public sealed class TaskItem : AggregateRoot<TaskId>
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    // Nominal spacing between adjacent OrderKeys. Large enough to absorb many
    // midpoint inserts before a rebalance is needed (log2(1000) ≈ 10 inserts).
    public const decimal OrderKeyStep = 1000m;

    // When a midpoint insert produces a gap smaller than this, the application
    // layer triggers a rebalance of the user's whole task list.
    public const decimal OrderKeyRebalanceThreshold = 0.001m;

    // List<Guid> rather than HashSet so EF 8's PrimitiveCollection can persist it
    // as a JSON array (PrimitiveCollection requires IList<T>). Uniqueness is
    // enforced in ReplaceTags; surfaced as TagId through the public TagIds projection.
    private readonly List<Guid> _tagIds = [];

    private TaskItem(
        TaskId id,
        Guid ownerId,
        string title,
        string? description,
        TaskItemStatus status,
        TaskPriority priority,
        DateTime? dueDateUtc,
        DateTime createdAtUtc,
        decimal orderKey)
        : base(id)
    {
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Status = status;
        Priority = priority;
        DueDateUtc = dueDateUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        OrderKey = orderKey;
    }

    // EF Core materialization constructor.
    private TaskItem()
    {
        Title = string.Empty;
        Status = TaskItemStatus.Pending;
        Priority = TaskPriority.Medium;
    }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DateTime? DueDateUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    // Decimal sort key used for manual (drag-and-drop) ordering. Lower first.
    public decimal OrderKey { get; private set; }

    public IEnumerable<TagId> TagIds => _tagIds.Select(static g => new TagId(g));

    public static ErrorOr<TaskItem> Create(
        Guid ownerId,
        string title,
        string? description,
        TaskPriority priority,
        DateTime? dueDateUtc,
        DateTime nowUtc,
        decimal orderKey)
    {
        if (ownerId == Guid.Empty)
        {
            return TaskErrors.OwnerRequired;
        }

        var validation = ValidateInputs(title, description, dueDateUtc, nowUtc, enforcePastDate: true);
        if (validation.IsError)
        {
            return validation.Errors;
        }

        var task = new TaskItem(
            TaskId.New(),
            ownerId,
            title.Trim(),
            NormalizeDescription(description),
            TaskItemStatus.Pending,
            priority,
            dueDateUtc,
            nowUtc,
            orderKey);

        task.RaiseDomainEvent(new TaskCreatedDomainEvent(task.Id, task.OwnerId, task.Title));
        return task;
    }

    public ErrorOr<Success> UpdateDetails(
        string title,
        string? description,
        TaskPriority priority,
        DateTime? dueDateUtc,
        DateTime nowUtc)
    {
        // Only enforce "no past due-date" when the caller actually changed the
        // due date. Editing the title of a task whose existing due date has
        // since rolled into the past would otherwise be impossible.
        var enforcePastDate = dueDateUtc != DueDateUtc;

        var validation = ValidateInputs(title, description, dueDateUtc, nowUtc, enforcePastDate);
        if (validation.IsError)
        {
            return validation.Errors;
        }

        Title = title.Trim();
        Description = NormalizeDescription(description);
        Priority = priority;
        DueDateUtc = dueDateUtc;
        UpdatedAtUtc = nowUtc;
        return Result.Success;
    }

    public ErrorOr<Success> Complete(DateTime nowUtc)
    {
        if (Status == TaskItemStatus.Completed)
        {
            return TaskErrors.AlreadyCompleted;
        }

        Status = TaskItemStatus.Completed;
        CompletedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new TaskCompletedDomainEvent(Id, nowUtc));
        return Result.Success;
    }

    public ErrorOr<Success> Reopen(DateTime nowUtc)
    {
        if (Status != TaskItemStatus.Completed)
        {
            return TaskErrors.NotCompleted;
        }

        Status = TaskItemStatus.Pending;
        CompletedAtUtc = null;
        UpdatedAtUtc = nowUtc;
        return Result.Success;
    }

    public ErrorOr<Success> ChangePriority(TaskPriority priority, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(priority);

        if (priority == Priority)
        {
            return Result.Success;
        }

        Priority = priority;
        UpdatedAtUtc = nowUtc;
        return Result.Success;
    }

    public ErrorOr<Success> ChangeStatus(TaskItemStatus status, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status == Status)
        {
            return Result.Success;
        }

        if (status == TaskItemStatus.Completed)
        {
            return Complete(nowUtc);
        }

        if (Status == TaskItemStatus.Completed && status != TaskItemStatus.Completed)
        {
            // Reopening path — reset CompletedAtUtc, then apply new status.
            CompletedAtUtc = null;
        }

        Status = status;
        UpdatedAtUtc = nowUtc;
        return Result.Success;
    }

    public void MoveTo(decimal orderKey, DateTime nowUtc)
    {
        OrderKey = orderKey;
        UpdatedAtUtc = nowUtc;
    }

    public void ReplaceTags(IEnumerable<TagId> tagIds, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(tagIds);
        _tagIds.Clear();
        foreach (var value in tagIds.Select(t => t.Value).Distinct())
        {
            _tagIds.Add(value);
        }

        UpdatedAtUtc = nowUtc;
    }

    // Infrastructure-facing: remove a tag association without replacing the full set.
    // Used when a Tag aggregate is deleted so associated tasks lose the reference.
    public bool RemoveTag(TagId tagId, DateTime nowUtc)
    {
        var removed = _tagIds.Remove(tagId.Value);
        if (removed)
        {
            UpdatedAtUtc = nowUtc;
        }

        return removed;
    }

    private static ErrorOr<Success> ValidateInputs(
        string title,
        string? description,
        DateTime? dueDateUtc,
        DateTime nowUtc,
        bool enforcePastDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return TaskErrors.TitleRequired;
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            return TaskErrors.TitleTooLong(MaxTitleLength);
        }

        if (description is not null && description.Length > MaxDescriptionLength)
        {
            return TaskErrors.DescriptionTooLong(MaxDescriptionLength);
        }

        if (enforcePastDate && dueDateUtc.HasValue && dueDateUtc.Value.Date < nowUtc.Date)
        {
            return TaskErrors.DueDateInPast;
        }

        return Result.Success;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
