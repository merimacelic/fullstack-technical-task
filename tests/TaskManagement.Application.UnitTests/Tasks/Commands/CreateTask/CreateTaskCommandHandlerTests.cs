using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Application.Tasks.Ordering;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_WithValidCommand_Should_Persist_And_ReturnResponse()
    {
        using var dbContext = InMemoryApplicationDbContext.CreateNew();
        var clock = new FakeDateTimeProvider(FixedNow);
        var handler = new CreateTaskCommandHandler(dbContext, clock, FakeCurrentUser.WithId(Owner), new OrderKeyService(dbContext, clock));
        var command = new CreateTaskCommand(
            Title: "Design review",
            Description: "Discuss the new ICON branding",
            Priority: "High",
            DueDateUtc: FixedNow.AddDays(5));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Title.ShouldBe("Design review");
        result.Value.Status.ShouldBe(TaskItemStatus.Pending.Name);
        (await dbContext.Tasks.CountAsync()).ShouldBe(1);
        (await dbContext.Tasks.SingleAsync()).OwnerId.ShouldBe(Owner);
    }

    [Fact]
    public async Task Handle_WithPastDueDate_Should_ReturnValidationError_AndNotPersist()
    {
        using var dbContext = InMemoryApplicationDbContext.CreateNew();
        var clock = new FakeDateTimeProvider(FixedNow);
        var handler = new CreateTaskCommandHandler(dbContext, clock, FakeCurrentUser.WithId(Owner), new OrderKeyService(dbContext, clock));
        var command = new CreateTaskCommand("Stale", null, "Low", FixedNow.AddDays(-1));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Task.DueDateInPast");
        (await dbContext.Tasks.CountAsync()).ShouldBe(0);
    }
}
