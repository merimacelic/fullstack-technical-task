using TaskManagement.Application.Tasks.Commands.ReopenTask;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.ReopenTask;

public class ReopenTaskCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_Should_Reopen_CompletedTask()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        task.Complete(FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ReopenTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow.AddHours(1)), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new ReopenTaskCommand(task.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Status.ShouldBe(TaskItemStatus.Pending.Name);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_NotCompleted()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ReopenTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new ReopenTaskCommand(task.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotCompleted");
    }
}
