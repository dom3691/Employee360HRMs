using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Payroll.SalaryStructures;

public sealed record SalaryStructureDto(
    Guid Id,
    string Name,
    decimal Basic,
    decimal Housing,
    decimal Transport,
    decimal OtherAllowances,
    decimal GrossSalary,
    bool IsActive);

public sealed record GetSalaryStructuresPagedQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<SalaryStructureDto>>>;

public sealed class GetSalaryStructuresPagedValidator : AbstractValidator<GetSalaryStructuresPagedQuery>
{
    public GetSalaryStructuresPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

public sealed class GetSalaryStructuresPagedHandler
    : IRequestHandler<GetSalaryStructuresPagedQuery, Result<PagedResult<SalaryStructureDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSalaryStructuresPagedHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<SalaryStructureDto>>> Handle(
        GetSalaryStructuresPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.SalaryStructures.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => Map(s))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<SalaryStructureDto>(items, request.Page, request.PageSize, total));
    }

    internal static SalaryStructureDto Map(SalaryStructure s) =>
        new(s.Id, s.Name, s.Basic, s.Housing, s.Transport, s.OtherAllowances, s.GrossSalary, s.IsActive);
}

public sealed record CreateSalaryStructureCommand(
    string Name,
    decimal Basic,
    decimal Housing,
    decimal Transport,
    decimal OtherAllowances) : IRequest<Result<Guid>>;

public sealed class CreateSalaryStructureValidator : AbstractValidator<CreateSalaryStructureCommand>
{
    public CreateSalaryStructureValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Basic).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Housing).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Transport).GreaterThanOrEqualTo(0);
        RuleFor(c => c.OtherAllowances).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateSalaryStructureHandler : IRequestHandler<CreateSalaryStructureCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateSalaryStructureHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateSalaryStructureCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _context.SalaryStructures.AnyAsync(s => s.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Result.Failure<Guid>($"A salary structure named '{name}' already exists.");
        }

        var structure = new SalaryStructure
        {
            Name = name,
            Basic = request.Basic,
            Housing = request.Housing,
            Transport = request.Transport,
            OtherAllowances = request.OtherAllowances,
        };

        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(structure.Id);
    }
}

public sealed record UpdateSalaryStructureCommand(
    Guid Id,
    string Name,
    decimal Basic,
    decimal Housing,
    decimal Transport,
    decimal OtherAllowances,
    bool IsActive) : IRequest<Result>;

public sealed class UpdateSalaryStructureValidator : AbstractValidator<UpdateSalaryStructureCommand>
{
    public UpdateSalaryStructureValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Basic).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Housing).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Transport).GreaterThanOrEqualTo(0);
        RuleFor(c => c.OtherAllowances).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSalaryStructureHandler : IRequestHandler<UpdateSalaryStructureCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateSalaryStructureHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateSalaryStructureCommand request, CancellationToken cancellationToken)
    {
        var structure = await _context.SalaryStructures.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

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
        structure.Basic = request.Basic;
        structure.Housing = request.Housing;
        structure.Transport = request.Transport;
        structure.OtherAllowances = request.OtherAllowances;
        structure.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteSalaryStructureCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteSalaryStructureHandler : IRequestHandler<DeleteSalaryStructureCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteSalaryStructureHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteSalaryStructureCommand request, CancellationToken cancellationToken)
    {
        var structure = await _context.SalaryStructures.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (structure is null)
        {
            return Result.Failure("Salary structure not found.");
        }

        structure.IsDeleted = true;
        structure.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
