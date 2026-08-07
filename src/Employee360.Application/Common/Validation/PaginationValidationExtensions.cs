using Employee360.Application.Common.Models;
using FluentValidation;

namespace Employee360.Application.Common.Validation;

/// <summary>Reusable FluentValidation rules for page and pageSize query parameters.</summary>
public static class PaginationValidationExtensions
{
    public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> ruleBuilder) =>
        ruleBuilder
            .GreaterThanOrEqualTo(PaginationConstants.MinPage)
            .WithMessage($"Page must be {PaginationConstants.MinPage} or greater.");

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> ruleBuilder) =>
        ruleBuilder
            .InclusiveBetween(PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize)
            .WithMessage(
                $"Page size must be between {PaginationConstants.MinPageSize} and {PaginationConstants.MaxPageSize}.");
}
