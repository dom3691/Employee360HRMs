namespace Employee360.Application.Common.Models;

/// <summary>Shared pagination limits for all list endpoints.</summary>
public static class PaginationConstants
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
