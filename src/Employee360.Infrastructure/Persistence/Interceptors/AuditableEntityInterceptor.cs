using System.Text.Json;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Employee360.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor enforcing the persistence cross-cutting rules:
/// <list type="bullet">
/// <item>Populates CreatedBy/CreatedAt on Added and ModifiedBy/ModifiedAt on Modified
/// <see cref="AuditableEntity"/> rows using <see cref="ICurrentUserService"/> and
/// <see cref="IDateTimeProvider"/>.</item>
/// <item>Converts hard deletes of <see cref="ISoftDelete"/> entities into soft deletes
/// (IsDeleted + DeletedAt), preserving HR data for retention (PRD NFR-RET-002).</item>
/// <item>Writes an <see cref="AuditLog"/> row per change with JSON old/new value
/// snapshots (PRD NFR-AUD-001).</item>
/// </list>
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditableEntityInterceptor(
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.Email ?? userId?.ToString();

        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>().ToList())
        {
            // The audit trail itself is never audited (prevents recursion) or edited.
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                _ => "Updated",
            };

            // Soft-delete conversion: flip the delete into an update before snapshots.
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = utcNow;
            }

            // Audit columns.
            if (entry.Entity is AuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedBy = userName;
                    auditable.CreatedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.ModifiedBy = userName;
                    auditable.ModifiedAt = utcNow;
                }
            }

            auditLogs.Add(CreateAuditLog(entry, action, userId, utcNow));
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private static AuditLog CreateAuditLog(
        EntityEntry<BaseEntity> entry,
        string action,
        Guid? userId,
        DateTime utcNow)
    {
        Dictionary<string, object?>? oldValues = null;
        Dictionary<string, object?>? newValues = null;

        switch (action)
        {
            case "Created":
                newValues = entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p]);
                break;

            case "Deleted":
                oldValues = entry.OriginalValues.Properties
                    .ToDictionary(p => p.Name, p => entry.OriginalValues[p]);
                break;

            default:
                var changedProperties = entry.Properties
                    .Where(p => p.IsModified)
                    .ToList();
                oldValues = changedProperties
                    .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                newValues = changedProperties
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                break;
        }

        return new AuditLog
        {
            EntityName = entry.Entity.GetType().Name,
            EntityId = entry.Entity.Id.ToString(),
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            UserId = userId,
            Timestamp = utcNow,
        };
    }
}
