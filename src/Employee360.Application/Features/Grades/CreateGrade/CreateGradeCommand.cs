using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.CreateGrade;

/// <summary>Creates a salary grade.</summary>
public sealed record CreateGradeCommand(
    string Title,
    string Code,
    string LevelRank,
    string? Description,
    decimal SalaryMin,
    decimal SalaryMax) : IRequest<Result<Guid>>;
