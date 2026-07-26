using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.GetGrades;

/// <summary>List row for grades.</summary>
public sealed record GradeListItem(
    Guid Id,
    string Name,
    int Level,
    decimal MinSalary,
    decimal MaxSalary,
    int PositionCount);

/// <summary>Paged grade list ordered by level.</summary>
public sealed record GetGradesPagedQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<GradeListItem>>>;

/// <summary>Fetches one grade by id.</summary>
public sealed record GetGradeByIdQuery(Guid GradeId) : IRequest<Result<GradeListItem>>;
