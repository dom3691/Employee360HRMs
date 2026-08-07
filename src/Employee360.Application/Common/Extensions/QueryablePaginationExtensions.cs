using Employee360.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Common.Extensions;

/// <summary>EF Core helpers for building <see cref="PagedResult{T}"/> responses.</summary>
public static class QueryablePaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
