using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Roles.CreateRole;

/// <summary>Handles <see cref="CreateRoleCommand"/> with a unique-name guard.</summary>
public sealed class CreateRoleHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var nameTaken = await _context.Roles
            .AnyAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

        if (nameTaken)
        {
            return Result.Failure<Guid>($"A role named '{name}' already exists.");
        }

        var role = new Role
        {
            Name = name,
            Description = request.Description.Trim(),
            IsSystemRole = false,
        };

        _context.Roles.Add(role);

        // Role is an AuditableEntity, so the audit interceptor records the create.
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(role.Id);
    }
}
