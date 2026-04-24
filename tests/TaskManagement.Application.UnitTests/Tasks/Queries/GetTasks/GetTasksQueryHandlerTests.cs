using TaskManagement.Application.Tasks.Queries.GetTasks;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Queries.GetTasks;

public class GetTasksQueryHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOwner = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_Should_ReturnTasksMatchingStatusFilter()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var pending = TestTaskFactory.New(Owner, "a", nowUtc: FixedNow);
        var done = TestTaskFactory.New(Owner, "b", nowUtc: FixedNow);
        done.Complete(FixedNow);
        db.Tasks.Add(pending);
        db.Tasks.Add(done);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTasksQueryHandler(db, FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new GetTasksQuery(Statuses: ["Completed"]),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Title.ShouldBe("b");
        result.Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnTasksMatchingAnyOfSelectedStatuses()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var pending = TestTaskFactory.New(Owner, "a", nowUtc: FixedNow);
        var inProgress = TestTaskFactory.New(Owner, "b", nowUtc: FixedNow.AddMinutes(1));
        inProgress.ChangeStatus(TaskItemStatus.InProgress, FixedNow.AddMinutes(1));
        var done = TestTaskFactory.New(Owner, "c", nowUtc: FixedNow.AddMinutes(2));
        done.Complete(FixedNow.AddMinutes(2));
        db.Tasks.Add(pending);
        db.Tasks.Add(inProgress);
        db.Tasks.Add(done);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTasksQueryHandler(db, FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new GetTasksQuery(Statuses: ["Pending", "Completed"]),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Items.Select(t => t.Title).ShouldBe(["a", "c"], ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_Should_FailFast_OnUnknownStatus()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var handler = new GetTasksQueryHandler(db, FakeCurrentUser.WithId(Owner));

        var result = await handler.Handle(
            new GetTasksQuery(Statuses: ["Pending", "nope"]),
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.UnknownStatus");
    }

    [Fact]
    public async Task Handle_Should_ApplyPagination()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        for (var i = 0; i < 5; i++)
        {
            db.Tasks.Add(TestTaskFactory.New(Owner, $"t{i}", nowUtc: FixedNow.AddMinutes(i)));
        }

        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTasksQueryHandler(db, FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(
            new GetTasksQuery(Page: 2, PageSize: 2),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Page.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(5);
        result.Value.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_Should_NotReturn_OtherUsersTasks()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        db.Tasks.Add(TestTaskFactory.New(Owner, "mine", nowUtc: FixedNow));
        db.Tasks.Add(TestTaskFactory.New(OtherOwner, "theirs", nowUtc: FixedNow));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTasksQueryHandler(db, FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new GetTasksQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Title.ShouldBe("mine");
    }
}
