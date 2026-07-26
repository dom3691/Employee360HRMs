using Employee360.Application.Common.Behaviours;
using Employee360.Domain.Common;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace Employee360.UnitTests.Application;

/// <summary>
/// Tests for <see cref="ValidationBehaviour{TRequest,TResponse}"/>: Result-typed
/// handlers receive failures; other response types throw ValidationException.
/// </summary>
public class ValidationBehaviourTests
{
    private sealed record CreateThingCommand(string Name) : IRequest<Result<Guid>>;

    private sealed class CreateThingValidator : AbstractValidator<CreateThingCommand>
    {
        public CreateThingValidator()
        {
            RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    private sealed record NonResultCommand(string Name) : IRequest<string>;

    private sealed class NonResultValidator : AbstractValidator<NonResultCommand>
    {
        public NonResultValidator()
        {
            RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    private sealed record PlainResultCommand(string Name) : IRequest<Result>;

    private sealed class PlainResultValidator : AbstractValidator<PlainResultCommand>
    {
        public PlainResultValidator()
        {
            RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    [Fact]
    public async Task InvalidRequest_WithGenericResultResponse_ShouldReturnFailure()
    {
        var behaviour = new ValidationBehaviour<CreateThingCommand, Result<Guid>>(
            [new CreateThingValidator()]);

        var result = await behaviour.Handle(
            new CreateThingCommand(""),
            () => Task.FromResult(Result.Success(Guid.NewGuid())),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Name is required.");
    }

    [Fact]
    public async Task InvalidRequest_WithPlainResultResponse_ShouldReturnFailure()
    {
        var behaviour = new ValidationBehaviour<PlainResultCommand, Result>(
            [new PlainResultValidator()]);

        var result = await behaviour.Handle(
            new PlainResultCommand(""),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task InvalidRequest_WithNonResultResponse_ShouldThrowValidationException()
    {
        var behaviour = new ValidationBehaviour<NonResultCommand, string>(
            [new NonResultValidator()]);

        var act = () => behaviour.Handle(
            new NonResultCommand(""),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ValidRequest_ShouldInvokeHandler()
    {
        var behaviour = new ValidationBehaviour<CreateThingCommand, Result<Guid>>(
            [new CreateThingValidator()]);
        var expected = Guid.NewGuid();

        var result = await behaviour.Handle(
            new CreateThingCommand("Valid name"),
            () => Task.FromResult(Result.Success(expected)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task NoValidators_ShouldInvokeHandler()
    {
        var behaviour = new ValidationBehaviour<CreateThingCommand, Result<Guid>>([]);

        var result = await behaviour.Handle(
            new CreateThingCommand(""),
            () => Task.FromResult(Result.Success(Guid.NewGuid())),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
