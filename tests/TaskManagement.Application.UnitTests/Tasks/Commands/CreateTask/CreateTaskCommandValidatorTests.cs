using TaskManagement.Application.Resources;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Application.UnitTests.Common;

namespace TaskManagement.Application.UnitTests.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new(new FakeStringLocalizer<SharedResource>());

    [Fact]
    public void Validate_ValidCommand_Should_Pass()
    {
        var command = new CreateTaskCommand("Ship it", null, "High", null);
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankTitle_Should_Fail(string title)
    {
        var result = _validator.Validate(new CreateTaskCommand(title, null, "Low", null));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_UnknownPriority_Should_Fail()
    {
        var result = _validator.Validate(new CreateTaskCommand("x", null, "SuperDuper", null));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateTaskCommand.Priority));
    }
}
