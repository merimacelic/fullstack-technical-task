using TaskManagement.Application.Tasks.Commands.ChangeTaskStatus;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.ChangeTaskStatus;

public class ChangeTaskStatusCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_Should_MoveTaskToInProgress()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeTaskStatusCommandHandler(
            db, new FakeDateTimeProvider(FixedNow.AddMinutes(5)), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new ChangeTaskStatusCommand(task.Id.Value, TaskItemStatus.InProgress.Name),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Status.ShouldBe(TaskItemStatus.InProgress.Name);
    }

    [Fact]
    public async Task Handle_Should_ResetCompletedAt_WhenMovingAwayFromCompleted()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        task.Complete(FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeTaskStatusCommandHandler(
            db, new FakeDateTimeProvider(FixedNow.AddMinutes(5)), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new ChangeTaskStatusCommand(task.Id.Value, TaskItemStatus.InProgress.Name),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Status.ShouldBe(TaskItemStatus.InProgress.Name);
        result.Value.CompletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_ForOtherUsersTask()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Other, "t", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ChangeTaskStatusCommandHandler(
            db, new FakeDateTimeProvider(FixedNow), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new ChangeTaskStatusCommand(task.Id.Value, TaskItemStatus.InProgress.Name),
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }
}
