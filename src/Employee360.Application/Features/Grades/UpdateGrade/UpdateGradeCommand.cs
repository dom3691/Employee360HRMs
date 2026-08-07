using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.UpdateGrade;

/// <summary>Updates a salary grade.</summary>
public sealed record UpdateGradeCommand(
    Guid GradeId,
    string Title,
    string Code,
    string LevelRank,
    string? Description,
    decimal SalaryMin,
    decimal SalaryMax) : IRequest<Result>;
