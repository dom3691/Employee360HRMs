using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.CreateLeaveType;

/// <summary>Handles <see cref="CreateLeaveTypeCommand"/> with unique name/code guards.</summary>
public sealed class CreateLeaveTypeHandler : IRequestHandler<CreateLeaveTypeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateLeaveTypeHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _context.LeaveTypes.AnyAsync(
                t => t.Name.ToLower() == name.ToLower() || t.Code == code, cancellationToken))
        {
            return Result.Failure<Guid>($"A leave type with name '{name}' or code '{code}' already exists.");
        }

        var leaveType = new LeaveType
        {
            Name = name,
            Code = code,
            IsPaid = request.IsPaid,
            RequiresAttachment = request.RequiresAttachment,
            Color = request.Color,
            IsActive = true,
        };

        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(leaveType.Id);
    }
}
