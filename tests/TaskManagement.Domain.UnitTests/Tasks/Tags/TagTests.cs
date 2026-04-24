using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Domain.UnitTests.Tasks.Tags;

public class TagTests
{
    private static readonly DateTime Now = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Create_Should_TrimName_And_StoreOwner()
    {
        var result = Tag.Create(Owner, "  urgent  ", Now);

        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("urgent");
        result.Value.OwnerId.ShouldBe(Owner);
        result.Value.CreatedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Create_WithEmptyOwner_Should_Fail()
    {
        var result = Tag.Create(Guid.Empty, "x", Now);
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TagErrors.OwnerRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Should_Fail(string name)
    {
        var result = Tag.Create(Owner, name, Now);
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TagErrors.NameRequired);
    }

    [Fact]
    public void Create_WithTooLongName_Should_Fail()
    {
        var tooLong = new string('a', Tag.MaxNameLength + 1);

        var result = Tag.Create(Owner, tooLong, Now);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tag.NameTooLong");
    }

    [Fact]
    public void Rename_Should_UpdateName()
    {
        var tag = Tag.Create(Owner, "old", Now).Value;

        var result = tag.Rename("new");

        result.IsError.ShouldBeFalse();
        tag.Name.ShouldBe("new");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithBlank_Should_Fail(string name)
    {
        var tag = Tag.Create(Owner, "old", Now).Value;

        var result = tag.Rename(name);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TagErrors.NameRequired);
    }
}
