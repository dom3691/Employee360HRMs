using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = Employee360.Domain.Entities.Company;

namespace Employee360.Application.Features.Company;

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/// <summary>Company profile response (FR-ADM-001).</summary>
public sealed record CompanyProfileDto(
    Guid Id,
    string Name,
    string? TradingName,
    string? LegalName,
    string? RCNumber,
    string? TIN,
    string? Industry,
    string? Website,
    string? Phone,
    string? Email,
    string? StreetAddress,
    string? City,
    string? State,
    string? Country,
    string? Address,
    string? LogoUrl,
    string DefaultCurrency);

// ---------------------------------------------------------------------------
// GetCompanyProfile
// ---------------------------------------------------------------------------

/// <summary>Gets the organization company profile.</summary>
public sealed record GetCompanyProfileQuery : IRequest<Result<CompanyProfileDto>>;

/// <summary>Handles <see cref="GetCompanyProfileQuery"/>.</summary>
public sealed class GetCompanyProfileHandler : IRequestHandler<GetCompanyProfileQuery, Result<CompanyProfileDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCompanyProfileHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<CompanyProfileDto>> Handle(
        GetCompanyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CompanyEntity.DefaultId, cancellationToken);

        if (company is null)
        {
            return Result.Failure<CompanyProfileDto>("Company profile has not been initialized.");
        }

        return Result.Success(Map(company));
    }

    internal static CompanyProfileDto Map(CompanyEntity company) =>
        new(
            company.Id,
            company.Name,
            company.TradingName,
            company.LegalName,
            company.RCNumber,
            company.TIN,
            company.Industry,
            company.Website,
            company.Phone,
            company.Email,
            company.StreetAddress,
            company.City,
            company.State,
            company.Country,
            company.Address,
            company.LogoUrl,
            company.DefaultCurrency);
}

// ---------------------------------------------------------------------------
// UpdateCompanyProfile
// ---------------------------------------------------------------------------

/// <summary>Updates the organization company profile (FR-ADM-001).</summary>
public sealed record UpdateCompanyProfileCommand(
    string Name,
    string? TradingName,
    string? LegalName,
    string? RCNumber,
    string? TIN,
    string? Industry,
    string? Website,
    string? Phone,
    string? Email,
    string? StreetAddress,
    string? City,
    string? State,
    string? Country,
    string? Address,
    string? LogoUrl,
    string DefaultCurrency) : IRequest<Result<CompanyProfileDto>>;

/// <summary>Input validation for <see cref="UpdateCompanyProfileCommand"/>.</summary>
public sealed class UpdateCompanyProfileValidator : AbstractValidator<UpdateCompanyProfileCommand>
{
    public UpdateCompanyProfileValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(256);

        RuleFor(c => c.RCNumber)
            .MaximumLength(64);

        RuleFor(c => c.TIN)
            .MaximumLength(32);

        RuleFor(c => c.Address)
            .MaximumLength(512);

        RuleFor(c => c.LogoUrl)
            .MaximumLength(2048);

        RuleFor(c => c.DefaultCurrency)
            .NotEmpty().WithMessage("Default currency is required.")
            .Length(3).WithMessage("Default currency must be a 3-letter ISO code.");
    }
}

/// <summary>Handles <see cref="UpdateCompanyProfileCommand"/>.</summary>
public sealed class UpdateCompanyProfileHandler
    : IRequestHandler<UpdateCompanyProfileCommand, Result<CompanyProfileDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateCompanyProfileHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<CompanyProfileDto>> Handle(
        UpdateCompanyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == CompanyEntity.DefaultId, cancellationToken);

        if (company is null)
        {
            company = new CompanyEntity { Id = CompanyEntity.DefaultId };
            _context.Companies.Add(company);
        }

        company.Name = request.Name.Trim();
        company.TradingName = request.TradingName?.Trim();
        company.LegalName = request.LegalName?.Trim();
        company.RCNumber = request.RCNumber?.Trim();
        company.TIN = request.TIN?.Trim();
        company.Industry = request.Industry?.Trim();
        company.Website = request.Website?.Trim();
        company.Phone = request.Phone?.Trim();
        company.Email = request.Email?.Trim();
        company.StreetAddress = request.StreetAddress?.Trim();
        company.City = request.City?.Trim();
        company.State = request.State?.Trim();
        company.Country = request.Country?.Trim();
        company.Address = request.Address?.Trim();
        company.LogoUrl = request.LogoUrl?.Trim();
        company.DefaultCurrency = request.DefaultCurrency.Trim().ToUpperInvariant();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(GetCompanyProfileHandler.Map(company));
    }
}
