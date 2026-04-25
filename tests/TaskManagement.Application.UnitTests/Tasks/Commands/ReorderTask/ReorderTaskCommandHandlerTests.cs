using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Tasks.Commands.ReorderTask;
using TaskManagement.Application.Tasks.Ordering;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.ReorderTask;

public class ReorderTaskCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Intruder = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly string[] ExpectedReorderedTitles = ["a", "moving", "b"];

    private static ReorderTaskCommandHandler CreateHandler(InMemoryApplicationDbContext db, Guid userId, DateTime now)
    {
        var clock = new FakeDateTimeProvider(now);
        var orderKeyService = new OrderKeyService(db, clock);
        var reorderSerializer = new PerOwnerReorderSerializer();
        return new ReorderTaskCommandHandler(db, clock, FakeCurrentUser.WithId(userId), orderKeyService, reorderSerializer);
    }

    [Fact]
    public async Task Handle_Should_Place_TargetBetweenNeighbours()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();

        var a = TaskItem.Create(Owner, "a", null, TaskPriority.Low, null, FixedNow, 1000m).Value;
        var b = TaskItem.Create(Owner, "b", null, TaskPriority.Low, null, FixedNow, 2000m).Value;
        var moving = TaskItem.Create(Owner, "moving", null, TaskPriority.Low, null, FixedNow, 5000m).Value;
        db.Tasks.AddRange(a, b, moving);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CreateHandler(db, Owner, FixedNow.AddMinutes(5));

        var result = await handler.Handle(
            new ReorderTaskCommand(moving.Id.Value, a.Id.Value, b.Id.Value),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        var reloaded = await db.Tasks.SingleAsync(t => t.Id == moving.Id);
        reloaded.OrderKey.ShouldBe(1500m);
    }

    [Fact]
    public async Task Handle_Should_Place_AtStart_When_PreviousIsNull()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var a = TaskItem.Create(Owner, "a", null, TaskPriority.Low, null, FixedNow, 1000m).Value;
        var moving = TaskItem.Create(Owner, "moving", null, TaskPriority.Low, null, FixedNow, 3000m).Value;
        db.Tasks.AddRange(a, moving);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CreateHandler(db, Owner, FixedNow);
        var result = await handler.Handle(
            new ReorderTaskCommand(moving.Id.Value, PreviousTaskId: null, NextTaskId: a.Id.Value),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        var reloaded = await db.Tasks.SingleAsync(t => t.Id == moving.Id);
        reloaded.OrderKey.ShouldBe(500m);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_TargetNotOwned()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var task = TaskItem.Create(Owner, "a", null, TaskPriority.Low, null, FixedNow, 1000m).Value;
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CreateHandler(db, Intruder, FixedNow);
        var result = await handler.Handle(
            new ReorderTaskCommand(task.Id.Value, null, null),
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_NeighbourNotOwned()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var mine = TaskItem.Create(Owner, "mine", null, TaskPriority.Low, null, FixedNow, 1000m).Value;
        var theirs = TaskItem.Create(Intruder, "theirs", null, TaskPriority.Low, null, FixedNow, 2000m).Value;
        db.Tasks.AddRange(mine, theirs);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CreateHandler(db, Owner, FixedNow);
        var result = await handler.Handle(
            new ReorderTaskCommand(mine.Id.Value, PreviousTaskId: theirs.Id.Value, NextTaskId: null),
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Rebalance_And_Produce_Key_Between_FreshNeighbours_When_GapCollapses()
    {
        // Regression guard for the old bug: BetweenAsync used to rebalance but then
        // return the midpoint from the *pre*-rebalance neighbour keys, which would
        // collide with the freshly renumbered list. After the fix, the returned
        // key must sort strictly between the post-rebalance neighbours.
        using var db = InMemoryApplicationDbContext.CreateNew();
        var a = TaskItem.Create(Owner, "a", null, TaskPriority.Low, null, FixedNow, 1.0000m).Value;
        var b = TaskItem.Create(Owner, "b", null, TaskPriority.Low, null, FixedNow, 1.0001m).Value;
        var moving = TaskItem.Create(Owner, "moving", null, TaskPriority.Low, null, FixedNow, 5000m).Value;
        db.Tasks.AddRange(a, b, moving);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = CreateHandler(db, Owner, FixedNow);
        var result = await handler.Handle(
            new ReorderTaskCommand(moving.Id.Value, a.Id.Value, b.Id.Value),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();

        var all = await db.Tasks.OrderBy(t => t.OrderKey).ToListAsync();
        var order = all.Select(t => t.Title).ToList();
        order.ShouldBe(ExpectedReorderedTitles);

        var aReloaded = await db.Tasks.SingleAsync(t => t.Id == a.Id);
        var movingReloaded = await db.Tasks.SingleAsync(t => t.Id == moving.Id);
        var bReloaded = await db.Tasks.SingleAsync(t => t.Id == b.Id);

        movingReloaded.OrderKey.ShouldBeGreaterThan(aReloaded.OrderKey);
        movingReloaded.OrderKey.ShouldBeLessThan(bReloaded.OrderKey);
    }

    // Regression guard for the race that the integration-test sibling
    // (TasksConcurrencyTests) catches against real SQL Server: two reorders into
    // the same gap, against a shared dataset and a shared singleton serializer,
    // must produce distinct OrderKeys.
    [Fact]
    public async Task Handle_Should_Produce_DistinctKeys_When_TwoReordersHitSameGap()
    {
        var dbName = Guid.NewGuid().ToString();

        // Seed the shared in-memory store once.
        await using (var seedDb = new InMemoryApplicationDbContext(
            new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            var a = TaskItem.Create(Owner, "A", null, TaskPriority.Low, null, FixedNow, 1000m).Value;
            var b = TaskItem.Create(Owner, "B", null, TaskPriority.Low, null, FixedNow, 2000m).Value;
            var c = TaskItem.Create(Owner, "C", null, TaskPriority.Low, null, FixedNow, 3000m).Value;
            var d = TaskItem.Create(Owner, "D", null, TaskPriority.Low, null, FixedNow, 4000m).Value;
            seedDb.Tasks.AddRange(a, b, c, d);
            await seedDb.SaveChangesAsync(CancellationToken.None);
        }

        Guid aId, bId, cId, dId;
        await using (var probe = new InMemoryApplicationDbContext(
            new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            aId = (await probe.Tasks.SingleAsync(t => t.Title == "A")).Id.Value;
            bId = (await probe.Tasks.SingleAsync(t => t.Title == "B")).Id.Value;
            cId = (await probe.Tasks.SingleAsync(t => t.Title == "C")).Id.Value;
            dId = (await probe.Tasks.SingleAsync(t => t.Title == "D")).Id.Value;
        }

        // Singleton across both requests — exactly how DI registers it in Program.cs.
        var sharedSerializer = new PerOwnerReorderSerializer();

        async Task<ErrorOr.ErrorOr<Application.Tasks.Responses.TaskResponse>> RunReorder(Guid moving)
        {
            await using var db = new InMemoryApplicationDbContext(
                new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options);
            var clock = new FakeDateTimeProvider(FixedNow);
            var orderKeyService = new OrderKeyService(db, clock);
            var handler = new ReorderTaskCommandHandler(
                db, clock, FakeCurrentUser.WithId(Owner), orderKeyService, sharedSerializer);
            return await handler.Handle(
                new ReorderTaskCommand(moving, aId, bId),
                CancellationToken.None);
        }

        var moveC = RunReorder(cId);
        var moveD = RunReorder(dId);
        await Task.WhenAll(moveC, moveD);

        (await moveC).IsError.ShouldBeFalse();
        (await moveD).IsError.ShouldBeFalse();

        await using var assertDb = new InMemoryApplicationDbContext(
            new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);
        var keys = await assertDb.Tasks
            .Where(t => t.OwnerId == Owner)
            .Select(t => t.OrderKey)
            .ToListAsync();
        keys.Count.ShouldBe(4);
        keys.Distinct().Count().ShouldBe(
            4,
            customMessage: "concurrent reorders into the same gap produced duplicate OrderKeys");
    }
}
