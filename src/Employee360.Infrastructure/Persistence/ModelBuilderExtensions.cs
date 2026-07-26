using System.Linq.Expressions;
using Employee360.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Infrastructure.Persistence;

/// <summary>
/// Model-building helpers shared by the runtime DbContext (and reusable in tests).
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds a global query filter <c>e =&gt; !e.IsDeleted</c> to every entity type
    /// implementing <see cref="ISoftDelete"/>, so soft-deleted rows are excluded
    /// from all queries unless <c>IgnoreQueryFilters()</c> is used.
    /// </summary>
    /// <param name="modelBuilder">The model builder being configured.</param>
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeletedProperty), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
