using Employee360.Application.Features.Grades.CreateGrade;
using Employee360.Application.Features.Grades.DeleteGrade;
using Employee360.Application.Features.Positions.CreatePosition;
using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Application.Grades;

/// <summary>Tests for grade/position slices: unique constraints, salary band, delete guards.</summary>
public class GradeAndPositionTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"grade-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    [Fact]
    public async Task CreateGrade_ShouldPersist()
    {
        await using var context = CreateContext();
        var handler = new CreateGradeHandler(context);

        var result = await handler.Handle(
            new CreateGradeCommand("Officer II", 3, 3_000_000m, 5_000_000m), default);

        result.IsSuccess.Should().BeTrue();
        var grade = await context.Grades.SingleAsync();
        grade.Level.Should().Be(3);
        grade.MaxSalary.Should().Be(5_000_000m);
    }

    [Fact]
    public async Task CreateGrade_DuplicateName_ShouldFail()
    {
        await using var context = CreateContext();
        var handler = new CreateGradeHandler(context);
        await handler.Handle(new CreateGradeCommand("Officer II", 3, 1m, 2m), default);

        var duplicate = await handler.Handle(
            new CreateGradeCommand("officer ii", 4, 1m, 2m), default);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateGrade_DuplicateLevel_ShouldFail()
    {
        await using var context = CreateContext();
        var handler = new CreateGradeHandler(context);
        await handler.Handle(new CreateGradeCommand("Officer II", 3, 1m, 2m), default);

        var duplicate = await handler.Handle(
            new CreateGradeCommand("Senior Officer", 3, 1m, 2m), default);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Should().Contain("level 3");
    }

    [Fact]
    public void CreateGrade_MinAboveMax_ShouldFailValidation()
    {
        var validator = new CreateGradeValidator();

        var result = validator.Validate(
            new CreateGradeCommand("Officer II", 3, 5_000_000m, 3_000_000m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("greater than or equal to minimum"));
    }

    [Fact]
    public async Task DeleteGrade_ReferencedByPosition_ShouldFail()
    {
        await using var context = CreateContext();
        var grade = new Grade { Name = "Officer II", Level = 3 };
        context.Grades.Add(grade);
        context.Positions.Add(new Position { Title = "Accountant", Code = "ACC", GradeId = grade.Id });
        await context.SaveChangesAsync();
        var handler = new DeleteGradeHandler(context);

        var result = await handler.Handle(new DeleteGradeCommand(grade.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("referenced by positions");
    }

    [Fact]
    public async Task CreatePosition_DuplicateCode_ShouldFail()
    {
        await using var context = CreateContext();
        context.Positions.Add(new Position { Title = "Accountant", Code = "ACC" });
        await context.SaveChangesAsync();
        var handler = new CreatePositionHandler(context);

        var result = await handler.Handle(
            new CreatePositionCommand("Senior Accountant", "acc", null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreatePosition_WithUnknownGrade_ShouldFail()
    {
        await using var context = CreateContext();
        var handler = new CreatePositionHandler(context);

        var result = await handler.Handle(
            new CreatePositionCommand("Accountant", "ACC", null, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Grade not found.");
    }
}
