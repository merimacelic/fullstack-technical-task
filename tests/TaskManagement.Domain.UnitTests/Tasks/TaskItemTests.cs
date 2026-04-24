using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Events;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Domain.UnitTests.Tasks;

public class TaskItemTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const decimal DefaultOrderKey = 1000m;

    [Fact]
    public void Create_WithValidInputs_Should_ReturnPendingTask_And_RaiseCreatedEvent()
    {
        var result = TaskItem.Create(
            ownerId: Owner,
            title: "Write tests",
            description: "Cover the TaskItem aggregate",
            priority: TaskPriority.High,
            dueDateUtc: FixedNow.AddDays(3),
            nowUtc: FixedNow,
            orderKey: DefaultOrderKey);

        result.IsError.ShouldBeFalse();
        var task = result.Value;
        task.OwnerId.ShouldBe(Owner);
        task.Title.ShouldBe("Write tests");
        task.Status.ShouldBe(TaskItemStatus.Pending);
        task.Priority.ShouldBe(TaskPriority.High);
        task.CreatedAtUtc.ShouldBe(FixedNow);
        task.OrderKey.ShouldBe(DefaultOrderKey);
        task.TagIds.ShouldBeEmpty();

        var evt = task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskCreatedDomainEvent>();
        evt.OwnerId.ShouldBe(Owner);
    }

    [Fact]
    public void Create_WithEmptyOwner_Should_Fail()
    {
        var result = TaskItem.Create(
            ownerId: Guid.Empty,
            title: "valid",
            description: null,
            priority: TaskPriority.Medium,
            dueDateUtc: null,
            nowUtc: FixedNow,
            orderKey: DefaultOrderKey);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TaskErrors.OwnerRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankTitle_Should_Fail(string? title)
    {
        var result = TaskItem.Create(
            ownerId: Owner,
            title: title!,
            description: null,
            priority: TaskPriority.Medium,
            dueDateUtc: null,
            nowUtc: FixedNow,
            orderKey: DefaultOrderKey);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TaskErrors.TitleRequired);
    }

    [Fact]
    public void Create_WithTooLongTitle_Should_Fail()
    {
        var longTitle = new string('a', TaskItem.MaxTitleLength + 1);

        var result = TaskItem.Create(Owner, longTitle, null, TaskPriority.Low, null, FixedNow, DefaultOrderKey);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.TitleTooLong");
    }

    [Fact]
    public void Create_WithPastDueDate_Should_Fail()
    {
        var result = TaskItem.Create(
            Owner,
            "valid",
            null,
            TaskPriority.Medium,
            FixedNow.AddDays(-1),
            FixedNow,
            DefaultOrderKey);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TaskErrors.DueDateInPast);
    }

    [Fact]
    public void Complete_Should_SetStatus_And_RaiseCompletedEvent()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        var result = task.Complete(FixedNow.AddHours(1));

        result.IsError.ShouldBeFalse();
        task.Status.ShouldBe(TaskItemStatus.Completed);
        task.CompletedAtUtc.ShouldBe(FixedNow.AddHours(1));
        task.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TaskCompletedDomainEvent>();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_Should_Fail()
    {
        var task = CreateValidTask();
        task.Complete(FixedNow).IsError.ShouldBeFalse();

        var result = task.Complete(FixedNow.AddMinutes(1));

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TaskErrors.AlreadyCompleted);
    }

    [Fact]
    public void Reopen_Should_ClearCompletion_And_SetPending()
    {
        var task = CreateValidTask();
        task.Complete(FixedNow).IsError.ShouldBeFalse();

        var result = task.Reopen(FixedNow.AddHours(2));

        result.IsError.ShouldBeFalse();
        task.Status.ShouldBe(TaskItemStatus.Pending);
        task.CompletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Reopen_WhenNotCompleted_Should_Fail()
    {
        var task = CreateValidTask();

        var result = task.Reopen(FixedNow.AddHours(1));

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TaskErrors.NotCompleted);
    }

    [Fact]
    public void UpdateDetails_Should_ApplyChanges_And_BumpUpdatedAt()
    {
        var task = CreateValidTask();

        var result = task.UpdateDetails(
            title: "New title",
            description: "new desc",
            priority: TaskPriority.Critical,
            dueDateUtc: FixedNow.AddDays(10),
            nowUtc: FixedNow.AddMinutes(5));

        result.IsError.ShouldBeFalse();
        task.Title.ShouldBe("New title");
        task.Description.ShouldBe("new desc");
        task.Priority.ShouldBe(TaskPriority.Critical);
        task.UpdatedAtUtc.ShouldBe(FixedNow.AddMinutes(5));
    }

    [Fact]
    public void MoveTo_Should_UpdateOrderKey_And_BumpUpdatedAt()
    {
        var task = CreateValidTask();
        var later = FixedNow.AddHours(2);

        task.MoveTo(2500m, later);

        task.OrderKey.ShouldBe(2500m);
        task.UpdatedAtUtc.ShouldBe(later);
    }

    [Fact]
    public void ReplaceTags_Should_SetTagIds_And_BumpUpdatedAt()
    {
        var task = CreateValidTask();
        var tag1 = TagId.New();
        var tag2 = TagId.New();

        task.ReplaceTags([tag1, tag2], FixedNow.AddMinutes(3));

        task.TagIds.Count().ShouldBe(2);
        task.TagIds.ShouldContain(tag1);
        task.TagIds.ShouldContain(tag2);
        task.UpdatedAtUtc.ShouldBe(FixedNow.AddMinutes(3));
    }

    [Fact]
    public void ReplaceTags_Should_Dedupe()
    {
        var task = CreateValidTask();
        var tag = TagId.New();

        task.ReplaceTags([tag, tag, tag], FixedNow);

        task.TagIds.Count().ShouldBe(1);
    }

    private static TaskItem CreateValidTask() =>
        TaskItem.Create(
            Owner,
            "Write tests",
            null,
            TaskPriority.Medium,
            FixedNow.AddDays(1),
            FixedNow,
            DefaultOrderKey).Value;
}
