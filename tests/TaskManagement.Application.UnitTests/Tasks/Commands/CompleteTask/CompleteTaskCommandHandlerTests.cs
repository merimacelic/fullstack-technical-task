using TaskManagement.Application.Tasks.Commands.CompleteTask;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.CompleteTask;

public class CompleteTaskCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_Should_CompleteTaskOwnedByUser()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CompleteTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow.AddMinutes(5)), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new CompleteTaskCommand(task.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Status.ShouldBe(TaskItemStatus.Completed.Name);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_AlreadyCompleted()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        task.Complete(FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CompleteTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new CompleteTaskCommand(task.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.AlreadyCompleted");
    }
}
