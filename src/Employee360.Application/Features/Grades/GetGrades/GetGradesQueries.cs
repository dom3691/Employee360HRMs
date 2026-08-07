using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.GetGrades;

/// <summary>Grade row aligned with frontend contract.</summary>
public sealed record GradeListItem(
    Guid Id,
    string Code,
    string LevelRank,
    string Title,
    string? Description,
    decimal SalaryMin,
    decimal SalaryMax);

/// <summary>Paged grade list ordered by level.</summary>
public sealed record GetGradesPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null)
    : IRequest<Result<PagedResult<GradeListItem>>>;

/// <summary>Fetches one grade by id.</summary>
public sealed record GetGradeByIdQuery(Guid GradeId) : IRequest<Result<GradeListItem>>;

internal static class GradeMapping
{
    internal static GradeListItem Map(Domain.Entities.Grade g) =>
        new(
            g.Id,
            g.Code,
            g.LevelRank,
            g.Name,
            g.Description,
            g.MinSalary,
            g.MaxSalary);

    internal static int ParseLevelRank(string levelRank)
    {
        var digits = new string(levelRank.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var level) ? level : 0;
    }
}
