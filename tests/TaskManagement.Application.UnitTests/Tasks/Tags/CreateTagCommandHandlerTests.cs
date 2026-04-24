using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Tasks.Tags.Commands.CreateTag;
using TaskManagement.Application.UnitTests.Common;

namespace TaskManagement.Application.UnitTests.Tasks.Tags;

public class CreateTagCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_Should_PersistTag()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var handler = new CreateTagCommandHandler(db, new FakeDateTimeProvider(Now), FakeCurrentUser.WithId(Owner));

        var result = await handler.Handle(new CreateTagCommand("urgent"), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("urgent");
        (await db.Tags.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_For_DuplicateName()
    {
        using var db = InMemoryApplicationDbContext.CreateNew();
        var handler = new CreateTagCommandHandler(db, new FakeDateTimeProvider(Now), FakeCurrentUser.WithId(Owner));
        (await handler.Handle(new CreateTagCommand("urgent"), CancellationToken.None)).IsError.ShouldBeFalse();

        var second = await handler.Handle(new CreateTagCommand("urgent"), CancellationToken.None);

        second.IsError.ShouldBeTrue();
        second.FirstError.Code.ShouldBe("Tag.AlreadyExists");
    }
}
