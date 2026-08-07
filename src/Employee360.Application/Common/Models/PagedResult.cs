namespace Employee360.Application.Common.Models;

/// <summary>Standard paginated payload returned by all list endpoints.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The current page of items.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Requested page size (max <see cref="PaginationConstants.MaxPageSize"/>).</param>
/// <param name="TotalCount">Total matching items across all pages.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>True when an earlier page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>True when a later page exists.</summary>
    public bool HasNextPage => Page < TotalPages;
}
