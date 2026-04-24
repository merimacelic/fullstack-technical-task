using TaskManagement.Application.Tasks.Queries.GetTaskById;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Intruder = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_Should_Return_Task_When_Owned()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTaskByIdQueryHandler(db, FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new GetTaskByIdQuery(task.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Title.ShouldBe("t");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_For_OtherUsersTask()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TestTaskFactory.New(Owner, "t", nowUtc: FixedNow);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTaskByIdQueryHandler(db, FakeCurrentUser.WithId(Intruder));
        var result = await handler.Handle(new GetTaskByIdQuery(task.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_DoesNotExist()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var handler = new GetTaskByIdQueryHandler(db, FakeCurrentUser.WithId(Owner));

        var result = await handler.Handle(new GetTaskByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }
}
