using Employee360.Application.Features.Auth.ChangePassword;
using Employee360.Application.Features.Auth.ResetPassword;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Auth;

/// <summary>
/// Tests for the FR-AUTH-002 password policy (min 8 chars, upper, lower, number,
/// special) as enforced by the reset/change password validators.
/// </summary>
public class PasswordPolicyTests
{
    private readonly ChangePasswordValidator _changeValidator = new();
    private readonly ResetPasswordValidator _resetValidator = new();

    [Theory]
    [InlineData("Sh0rt!", "at least 8 characters")]
    [InlineData("alllower1!", "uppercase letter")]
    [InlineData("ALLUPPER1!", "lowercase letter")]
    [InlineData("NoNumbers!", "number")]
    [InlineData("NoSpecial1", "special character")]
    [InlineData("", "required")]
    public void WeakPasswords_ShouldFailPolicy(string password, string expectedMessageFragment)
    {
        var result = _changeValidator.Validate(
            new ChangePasswordCommand("Current!1a", password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangePasswordCommand.NewPassword) &&
            e.ErrorMessage.Contains(expectedMessageFragment));
    }

    [Theory]
    [InlineData("Str0ng!Pass")]
    [InlineData("C0mplex#Passw0rd")]
    [InlineData("Naija@2026")]
    public void StrongPasswords_ShouldPassPolicy(string password)
    {
        var result = _changeValidator.Validate(
            new ChangePasswordCommand("Current!1a", password));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NewPassword_SameAsCurrent_ShouldFail()
    {
        var result = _changeValidator.Validate(
            new ChangePasswordCommand("Same!Pass1", "Same!Pass1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("different"));
    }

    [Fact]
    public void ResetPassword_AppliesSamePolicy()
    {
        var result = _resetValidator.Validate(
            new ResetPasswordCommand("a@b.ng", "token", "weak"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }
}
