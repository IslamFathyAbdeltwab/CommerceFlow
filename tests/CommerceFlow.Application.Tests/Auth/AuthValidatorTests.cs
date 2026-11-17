using CommerceFlow.Application.Auth.Commands.LoginUser;
using CommerceFlow.Application.Auth.Commands.RegisterUser;

namespace CommerceFlow.Application.Tests.Auth;

public sealed class AuthValidatorTests
{
    // ---------- RegisterUser ----------

    private static RegisterUserCommand ValidRegister() =>
        new("ada@shop.de", "Ada Lovelace", "S3cretPass");

    [Fact]
    public void Register_WithValidInput_IsValid()
    {
        var result = new RegisterUserCommandValidator().Validate(ValidRegister());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_WithEmptyEmail_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Email = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public void Register_WithNotAnEmail_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Email = "ada-at-shop" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public void Register_WithOverlongEmail_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Email = new string('a', 254) + "@shop.de" }); // 262 > 254

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public void Register_WithEmptyFullName_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { FullName = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.FullName));
    }

    [Fact]
    public void Register_WithOverlongFullName_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { FullName = new string('x', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.FullName));
    }

    [Fact]
    public void Register_WithEmptyPassword_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Password = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void Register_WithSevenCharacterPassword_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Password = "Ab3def" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void Register_WithDigitlessPassword_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Password = "abcdefghij" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorMessage.Contains("digit") && e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void Register_WithLetterlessPassword_IsInvalid()
    {
        var result = new RegisterUserCommandValidator()
            .Validate(ValidRegister() with { Password = "1234567890" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorMessage.Contains("letter") && e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    // ---------- LoginUser ----------

    private static LoginUserCommand ValidLogin() =>
        new("ada@shop.de", "S3cretPass");

    [Fact]
    public void Login_WithValidInput_IsValid()
    {
        var result = new LoginUserCommandValidator().Validate(ValidLogin());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Login_WithEmptyEmail_IsInvalid()
    {
        var result = new LoginUserCommandValidator()
            .Validate(ValidLogin() with { Email = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginUserCommand.Email));
    }

    [Fact]
    public void Login_WithNotAnEmail_IsInvalid()
    {
        var result = new LoginUserCommandValidator()
            .Validate(ValidLogin() with { Email = "not-an-email" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginUserCommand.Email));
    }

    [Fact]
    public void Login_WithEmptyPassword_IsInvalid()
    {
        var result = new LoginUserCommandValidator()
            .Validate(ValidLogin() with { Password = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginUserCommand.Password));
    }

    [Fact]
    public void Login_WithWeakPassword_IsStillValid()
    {
        // policy runs at registration only; re-checking it at login would
        // tell a guesser which rule their guess broke
        var result = new LoginUserCommandValidator()
            .Validate(ValidLogin() with { Password = "x" });

        Assert.True(result.IsValid);
    }
}
