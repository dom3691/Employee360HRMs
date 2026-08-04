using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Payroll.TaxBands;

public sealed record TaxBandDto(
    Guid Id,
    int TaxYear,
    int BandOrder,
    decimal? UpperBoundAnnual,
    decimal Rate,
    bool IsActive);

public sealed record GetTaxBandsQuery(int TaxYear) : IRequest<Result<IReadOnlyList<TaxBandDto>>>;

public sealed class GetTaxBandsHandler : IRequestHandler<GetTaxBandsQuery, Result<IReadOnlyList<TaxBandDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTaxBandsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<TaxBandDto>>> Handle(
        GetTaxBandsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.TaxBands
            .AsNoTracking()
            .Where(b => b.TaxYear == request.TaxYear)
            .OrderBy(b => b.BandOrder)
            .Select(b => new TaxBandDto(b.Id, b.TaxYear, b.BandOrder, b.UpperBoundAnnual, b.Rate, b.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TaxBandDto>>(items);
    }
}

public sealed record UpsertTaxBandCommand(
    Guid? Id,
    int TaxYear,
    int BandOrder,
    decimal? UpperBoundAnnual,
    decimal Rate,
    bool IsActive = true) : IRequest<Result<Guid>>;

public sealed class UpsertTaxBandValidator : AbstractValidator<UpsertTaxBandCommand>
{
    public UpsertTaxBandValidator()
    {
        RuleFor(c => c.TaxYear).GreaterThan(2000);
        RuleFor(c => c.BandOrder).GreaterThanOrEqualTo(1);
        RuleFor(c => c.Rate).InclusiveBetween(0, 1);
    }
}

public sealed class UpsertTaxBandHandler : IRequestHandler<UpsertTaxBandCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public UpsertTaxBandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(UpsertTaxBandCommand request, CancellationToken cancellationToken)
    {
        TaxBand band;

        if (request.Id.HasValue)
        {
            band = await _context.TaxBands.FirstOrDefaultAsync(b => b.Id == request.Id.Value, cancellationToken);

            if (band is null)
            {
                return Result.Failure<Guid>("Tax band not found.");
            }
        }
        else
        {
            band = new TaxBand();
            _context.TaxBands.Add(band);
        }

        if (await _context.TaxBands.AnyAsync(
                b => b.TaxYear == request.TaxYear &&
                     b.BandOrder == request.BandOrder &&
                     b.Id != band.Id,
                cancellationToken))
        {
            return Result.Failure<Guid>($"Band order {request.BandOrder} already exists for tax year {request.TaxYear}.");
        }

        band.TaxYear = request.TaxYear;
        band.BandOrder = request.BandOrder;
        band.UpperBoundAnnual = request.UpperBoundAnnual;
        band.Rate = request.Rate;
        band.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(band.Id);
    }
}

public sealed record DeleteTaxBandCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteTaxBandHandler : IRequestHandler<DeleteTaxBandCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteTaxBandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteTaxBandCommand request, CancellationToken cancellationToken)
    {
        var band = await _context.TaxBands.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (band is null)
        {
            return Result.Failure("Tax band not found.");
        }

        _context.TaxBands.Remove(band);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record StatutoryRateDto(
    Guid Id,
    string Code,
    string Name,
    int TaxYear,
    decimal? EmployeeRate,
    decimal? EmployerRate,
    decimal? FixedAnnualAmount,
    decimal? VariableRate,
    Domain.Enums.StatutoryRateBasis Basis,
    bool IsActive);

public sealed record GetStatutoryRatesQuery(int TaxYear) : IRequest<Result<IReadOnlyList<StatutoryRateDto>>>;

public sealed class GetStatutoryRatesHandler : IRequestHandler<GetStatutoryRatesQuery, Result<IReadOnlyList<StatutoryRateDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStatutoryRatesHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<StatutoryRateDto>>> Handle(
        GetStatutoryRatesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.PayrollStatutoryRates
            .AsNoTracking()
            .Where(r => r.TaxYear == request.TaxYear)
            .OrderBy(r => r.Code)
            .Select(r => new StatutoryRateDto(
                r.Id, r.Code, r.Name, r.TaxYear,
                r.EmployeeRate, r.EmployerRate, r.FixedAnnualAmount, r.VariableRate,
                r.Basis, r.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<StatutoryRateDto>>(items);
    }
}

public sealed record UpsertStatutoryRateCommand(
    Guid? Id,
    string Code,
    string Name,
    int TaxYear,
    decimal? EmployeeRate,
    decimal? EmployerRate,
    decimal? FixedAnnualAmount,
    decimal? VariableRate,
    Domain.Enums.StatutoryRateBasis Basis,
    bool IsActive = true) : IRequest<Result<Guid>>;

public sealed class UpsertStatutoryRateValidator : AbstractValidator<UpsertStatutoryRateCommand>
{
    public UpsertStatutoryRateValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.TaxYear).GreaterThan(2000);
    }
}

public sealed class UpsertStatutoryRateHandler : IRequestHandler<UpsertStatutoryRateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public UpsertStatutoryRateHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(UpsertStatutoryRateCommand request, CancellationToken cancellationToken)
    {
        PayrollStatutoryRate rate;

        if (request.Id.HasValue)
        {
            rate = await _context.PayrollStatutoryRates
                .FirstOrDefaultAsync(r => r.Id == request.Id.Value, cancellationToken);

            if (rate is null)
            {
                return Result.Failure<Guid>("Statutory rate not found.");
            }
        }
        else
        {
            rate = new PayrollStatutoryRate();
            _context.PayrollStatutoryRates.Add(rate);
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _context.PayrollStatutoryRates.AnyAsync(
                r => r.TaxYear == request.TaxYear && r.Code == code && r.Id != rate.Id,
                cancellationToken))
        {
            return Result.Failure<Guid>($"Rate code '{code}' already exists for tax year {request.TaxYear}.");
        }

        rate.Code = code;
        rate.Name = request.Name.Trim();
        rate.TaxYear = request.TaxYear;
        rate.EmployeeRate = request.EmployeeRate;
        rate.EmployerRate = request.EmployerRate;
        rate.FixedAnnualAmount = request.FixedAnnualAmount;
        rate.VariableRate = request.VariableRate;
        rate.Basis = request.Basis;
        rate.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(rate.Id);
    }
}
