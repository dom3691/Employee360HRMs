using Employee360.Domain.Common;
using FluentAssertions;

namespace Employee360.UnitTests.Domain;

/// <summary>Tests for the <see cref="Result"/> and <see cref="Result{T}"/> wrappers.</summary>
public class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccessful_WithNoErrors()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldBeFailure_WithErrors()
    {
        var result = Result.Failure("Leave balance is insufficient.");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("Leave balance is insufficient.");
        result.Error.Should().Be("Leave balance is insufficient.");
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldPreserveAll()
    {
        var result = Result.Failure("Error one.", "Error two.");

        result.Errors.Should().HaveCount(2);
        result.Error.Should().Be("Error one.");
    }

    [Fact]
    public void Failure_WithoutErrors_ShouldThrow()
    {
        var act = () => Result.Failure(Array.Empty<string>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must contain at least one error*");
    }

    [Fact]
    public void GenericSuccess_ShouldExposeValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ShouldThrow()
    {
        var result = Result.Failure<int>("Not found.");

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void ImplicitConversion_ShouldWrapValueAsSuccess()
    {
        Result<string> result = "EMP-00001";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("EMP-00001");
    }
}
