using TaskManagement.Application.Resources;
using TaskManagement.Application.UnitTests.Common;
using TaskManagement.Application.Users.Commands.RegisterUser;

namespace TaskManagement.Application.UnitTests.Users;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _sut = new(new FakeStringLocalizer<SharedResource>());

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("foo@")]
    public void Invalid_Email_Should_Fail(string email)
    {
        var result = _sut.Validate(new RegisterUserCommand(email, "Passw0rd!"));
        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Weak_Password_Should_Fail(string password)
    {
        var result = _sut.Validate(new RegisterUserCommand("user@icon.test", password));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _sut.Validate(new RegisterUserCommand("user@icon.test", "Passw0rd!"));
        result.IsValid.ShouldBeTrue();
    }
}
