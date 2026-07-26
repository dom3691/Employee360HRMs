using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.PublicHolidays;

// ---------------------------------------------------------------------------
// AddPublicHoliday (FR-ADM-002)
// ---------------------------------------------------------------------------

/// <summary>Adds a public holiday to the calendar.</summary>
public sealed record AddPublicHolidayCommand(DateOnly Date, string Name) : IRequest<Result<Guid>>;

/// <summary>Input validation for <see cref="AddPublicHolidayCommand"/>.</summary>
public sealed class AddPublicHolidayValidator : AbstractValidator<AddPublicHolidayCommand>
{
    public AddPublicHolidayValidator()
    {
        RuleFor(c => c.Date)
            .NotEmpty().WithMessage("Holiday date is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Holiday name is required.")
            .MaximumLength(128);
    }
}

/// <summary>Handles <see cref="AddPublicHolidayCommand"/> (unique per date).</summary>
public sealed class AddPublicHolidayHandler : IRequestHandler<AddPublicHolidayCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AddPublicHolidayHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddPublicHolidayCommand request, CancellationToken cancellationToken)
    {
        if (await _context.PublicHolidays.AnyAsync(h => h.Date == request.Date, cancellationToken))
        {
            return Result.Failure<Guid>($"A holiday already exists on {request.Date:dd/MM/yyyy}.");
        }

        var holiday = new Domain.Entities.PublicHoliday
        {
            Date = request.Date,
            Name = request.Name.Trim(),
            Year = request.Date.Year,
        };

        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(holiday.Id);
    }
}

// ---------------------------------------------------------------------------
// RemovePublicHoliday
// ---------------------------------------------------------------------------

/// <summary>Removes a public holiday by date.</summary>
public sealed record RemovePublicHolidayCommand(DateOnly Date) : IRequest<Result>;

/// <summary>Handles <see cref="RemovePublicHolidayCommand"/>.</summary>
public sealed class RemovePublicHolidayHandler : IRequestHandler<RemovePublicHolidayCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RemovePublicHolidayHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RemovePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(h => h.Date == request.Date, cancellationToken);

        if (holiday is null)
        {
            return Result.Failure($"No holiday exists on {request.Date:dd/MM/yyyy}.");
        }

        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// GetPublicHolidays
// ---------------------------------------------------------------------------

/// <summary>A public holiday row.</summary>
public sealed record PublicHolidayItem(Guid Id, DateOnly Date, string Name);

/// <summary>Lists public holidays for a year.</summary>
public sealed record GetPublicHolidaysQuery(int Year) : IRequest<Result<IReadOnlyList<PublicHolidayItem>>>;

/// <summary>Handles <see cref="GetPublicHolidaysQuery"/>.</summary>
public sealed class GetPublicHolidaysHandler
    : IRequestHandler<GetPublicHolidaysQuery, Result<IReadOnlyList<PublicHolidayItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetPublicHolidaysHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PublicHolidayItem>>> Handle(
        GetPublicHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        var holidays = await _context.PublicHolidays
            .AsNoTracking()
            .Where(h => h.Year == request.Year)
            .OrderBy(h => h.Date)
            .Select(h => new PublicHolidayItem(h.Id, h.Date, h.Name))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PublicHolidayItem>>(holidays);
    }
}
