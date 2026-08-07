using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.PublicHolidays;

public sealed record AddPublicHolidayCommand(
    DateOnly Date,
    string Name,
    string? Type = null,
    bool IsRecurring = false,
    string? Region = null) : IRequest<Result<Guid>>;

public sealed class AddPublicHolidayValidator : AbstractValidator<AddPublicHolidayCommand>
{
    public AddPublicHolidayValidator()
    {
        RuleFor(c => c.Date).NotEmpty().WithMessage("Holiday date is required.");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Type).MaximumLength(32);
        RuleFor(c => c.Region).MaximumLength(128);
    }
}

public sealed class AddPublicHolidayHandler : IRequestHandler<AddPublicHolidayCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AddPublicHolidayHandler(IApplicationDbContext context) => _context = context;

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
            HolidayType = request.Type?.Trim(),
            IsRecurring = request.IsRecurring,
            Region = request.Region?.Trim(),
        };

        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(holiday.Id);
    }
}

public sealed record UpdatePublicHolidayCommand(
    Guid Id,
    DateOnly Date,
    string Name,
    string? Type,
    bool IsRecurring,
    string? Region) : IRequest<Result>;

public sealed class UpdatePublicHolidayValidator : AbstractValidator<UpdatePublicHolidayCommand>
{
    public UpdatePublicHolidayValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdatePublicHolidayHandler : IRequestHandler<UpdatePublicHolidayCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdatePublicHolidayHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdatePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (holiday is null)
        {
            return Result.Failure("Public holiday not found.");
        }

        if (holiday.Date != request.Date &&
            await _context.PublicHolidays.AnyAsync(h => h.Date == request.Date && h.Id != request.Id, cancellationToken))
        {
            return Result.Failure($"A holiday already exists on {request.Date:dd/MM/yyyy}.");
        }

        holiday.Date = request.Date;
        holiday.Name = request.Name.Trim();
        holiday.Year = request.Date.Year;
        holiday.HolidayType = request.Type?.Trim();
        holiday.IsRecurring = request.IsRecurring;
        holiday.Region = request.Region?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record RemovePublicHolidayCommand(DateOnly Date) : IRequest<Result>;

public sealed class RemovePublicHolidayHandler : IRequestHandler<RemovePublicHolidayCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RemovePublicHolidayHandler(IApplicationDbContext context) => _context = context;

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

public sealed record DeletePublicHolidayByIdCommand(Guid Id) : IRequest<Result>;

public sealed class DeletePublicHolidayByIdHandler : IRequestHandler<DeletePublicHolidayByIdCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeletePublicHolidayByIdHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeletePublicHolidayByIdCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (holiday is null)
        {
            return Result.Failure("Public holiday not found.");
        }

        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record PublicHolidayItem(
    Guid Id,
    string Name,
    DateOnly Date,
    string? Type,
    bool IsRecurring,
    string? Region);

public sealed record GetPublicHolidaysQuery(int? Year = null, string? Region = null)
    : IRequest<Result<IReadOnlyList<PublicHolidayItem>>>;

public sealed class GetPublicHolidaysHandler
    : IRequestHandler<GetPublicHolidaysQuery, Result<IReadOnlyList<PublicHolidayItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetPublicHolidaysHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<PublicHolidayItem>>> Handle(
        GetPublicHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        var year = request.Year ?? DateTime.UtcNow.Year;
        var query = _context.PublicHolidays.AsNoTracking().Where(h => h.Year == year);

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            query = query.Where(h => h.Region == null || h.Region == region);
        }

        var holidays = await query
            .OrderBy(h => h.Date)
            .Select(h => new PublicHolidayItem(
                h.Id,
                h.Name,
                h.Date,
                h.HolidayType,
                h.IsRecurring,
                h.Region))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PublicHolidayItem>>(holidays);
    }
}
