using TaskManagement.Application.Tasks.Commands.UpdateTask;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Intruder = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_Should_UpdateTaskOwnedByUser()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "old", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow.AddMinutes(5)), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new UpdateTaskCommand(task.Id.Value, "new", "desc", "High", FixedNow.AddDays(1)),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Title.ShouldBe("new");
        result.Value.Priority.ShouldBe("High");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_OwnerMismatch()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "old", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow), FakeCurrentUser.WithId(Intruder));
        var result = await handler.Handle(
            new UpdateTaskCommand(task.Id.Value, "hijack", null, "High", null),
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_IdDoesNotExist()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var handler = new UpdateTaskCommandHandler(
            db, new FakeDateTimeProvider(FixedNow), FakeCurrentUser.WithId(Owner));

        var result = await handler.Handle(
            new UpdateTaskCommand(Guid.NewGuid(), "x", null, "Low", null),
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }
}
