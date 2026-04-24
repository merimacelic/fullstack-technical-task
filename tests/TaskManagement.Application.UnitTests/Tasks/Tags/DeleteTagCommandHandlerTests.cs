using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Tasks.Tags.Commands.DeleteTag;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.UnitTests.Tasks.Tags;

public class DeleteTagCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_Should_RemoveTag_AndSweepAssociationsFromTasks()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var tag = Tag.Create(Owner, "urgent", Now).Value;
        db.Tags.Add(tag);

        var task = TestTaskFactory.New(Owner, "t", nowUtc: Now);
        task.ReplaceTags([tag.Id], Now);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteTagCommandHandler(db, new FakeDateTimeProvider(Now.AddMinutes(1)), FakeCurrentUser.WithId(Owner));
        var result = await handler.Handle(new DeleteTagCommand(tag.Id.Value), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        (await db.Tags.CountAsync()).ShouldBe(0);
        var reloadedTask = await db.Tasks.SingleAsync();
        reloadedTask.TagIds.ShouldBeEmpty();
    }
}
