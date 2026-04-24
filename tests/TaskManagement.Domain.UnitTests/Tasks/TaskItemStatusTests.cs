using TaskManagement.Domain.Tasks;

namespace TaskManagement.Domain.UnitTests.Tasks;

public class TaskItemStatusTests
{
    [Fact]
    public void GetAll_Should_Return_All_Declared_Values()
    {
        var all = TaskItemStatus.GetAll().ToArray();

        all.ShouldContain(TaskItemStatus.Pending);
        all.ShouldContain(TaskItemStatus.InProgress);
        all.ShouldContain(TaskItemStatus.Completed);
        all.Length.ShouldBe(3);
    }

    [Fact]
    public void FromName_Should_BeCaseInsensitive()
    {
        TaskItemStatus.FromName("pending").ShouldBe(TaskItemStatus.Pending);
        TaskItemStatus.FromName("PENDING").ShouldBe(TaskItemStatus.Pending);
    }

    [Fact]
    public void FromName_UnknownValue_Should_Throw()
    {
        Should.Throw<InvalidOperationException>(() => TaskItemStatus.FromName("bogus"));
    }

    [Fact]
    public void Equality_Should_BeById()
    {
        var a = TaskItemStatus.Pending;
        var b = TaskItemStatus.FromId(1);

        (a == b).ShouldBeTrue();
        a.Equals(b).ShouldBeTrue();
    }
}
