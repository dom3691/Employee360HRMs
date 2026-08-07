using System.Text.Json;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Payroll.SalaryStructures;

public sealed record SalaryStructureComponentDto(
    string Name,
    string Calculation,
    decimal Percentage,
    bool IsTaxable,
    bool IsPensionable);

public sealed record UpdateSalaryStructureComponentsCommand(
    Guid Id,
    string Name,
    string GradeCodes,
    decimal GrossMonthly,
    IReadOnlyList<SalaryStructureComponentDto> Components) : IRequest<Result>;

public sealed class UpdateSalaryStructureComponentsValidator : AbstractValidator<UpdateSalaryStructureComponentsCommand>
{
    public UpdateSalaryStructureComponentsValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.GrossMonthly).GreaterThan(0);
        RuleFor(c => c.Components).NotEmpty();
        RuleFor(c => c.Components)
            .Must(components => components.Sum(c => c.Percentage) is >= 99m and <= 101m)
            .WithMessage("Component percentages must sum to approximately 100.");
        RuleForEach(c => c.Components).ChildRules(component =>
        {
            component.RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            component.RuleFor(x => x.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
        });
    }
}

public sealed class UpdateSalaryStructureComponentsHandler
    : IRequestHandler<UpdateSalaryStructureComponentsCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateSalaryStructureComponentsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateSalaryStructureComponentsCommand request, CancellationToken cancellationToken)
    {
        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (structure is null)
        {
            return Result.Failure("Salary structure not found.");
        }

        var name = request.Name.Trim();

        if (await _context.SalaryStructures.AnyAsync(
                s => s.Id != request.Id && s.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Result.Failure($"A salary structure named '{name}' already exists.");
        }

        structure.Name = name;
        structure.GradeCodes = request.GradeCodes.Trim();
        structure.ComponentsJson = JsonSerializer.Serialize(request.Components);
        SalaryStructureMapping.ApplyComponents(structure, request.GrossMonthly, request.Components);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class SalaryStructureMapping
{
    internal static SalaryStructureDto Map(SalaryStructure s, int employeeCount = 0)
    {
        var components = DeserializeComponents(s.ComponentsJson);
        var gross = s.GrossSalary;

        return new SalaryStructureDto(
            s.Id,
            s.Name,
            s.Basic,
            s.Housing,
            s.Transport,
            s.OtherAllowances,
            gross,
            s.IsActive,
            s.GradeCodes,
            employeeCount > 0 ? employeeCount : null,
            components,
            gross);
    }

    internal static void ApplyComponents(
        SalaryStructure structure,
        decimal grossMonthly,
        IReadOnlyList<SalaryStructureComponentDto> components)
    {
        decimal Amount(string componentName) =>
            components
                .Where(c => c.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                .Sum(c => grossMonthly * c.Percentage / 100m);

        structure.Basic = Amount("Basic");
        structure.Housing = Amount("Housing");
        structure.Transport = Amount("Transport");

        var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Basic", "Housing", "Transport",
        };

        structure.OtherAllowances = components
            .Where(c => !named.Contains(c.Name))
            .Sum(c => grossMonthly * c.Percentage / 100m);

        if (structure.Basic == 0 && structure.Housing == 0 && structure.Transport == 0 && structure.OtherAllowances == 0)
        {
            structure.Basic = grossMonthly;
        }
    }

    internal static IReadOnlyList<SalaryStructureComponentDto>? DeserializeComponents(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return BuildDefaultComponents();
        }

        return JsonSerializer.Deserialize<List<SalaryStructureComponentDto>>(json) ?? BuildDefaultComponents();
    }

    internal static IReadOnlyList<SalaryStructureComponentDto> BuildDefaultComponents() =>
    [
        new("Basic", "PercentOfGross", 50, true, true),
        new("Housing", "PercentOfGross", 30, true, true),
        new("Transport", "PercentOfGross", 10, true, false),
        new("Other", "PercentOfGross", 10, true, false),
    ];
}
