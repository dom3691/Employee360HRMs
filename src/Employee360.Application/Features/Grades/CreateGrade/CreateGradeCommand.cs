using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.CreateGrade;

/// <summary>Creates a salary grade (PRD data model: Grade).</summary>
/// <param name="Name">Unique grade name, e.g. "Officer II".</param>
/// <param name="Level">Unique numeric level (higher = more senior).</param>
/// <param name="MinSalary">Minimum annual gross salary (₦).</param>
/// <param name="MaxSalary">Maximum annual gross salary (₦).</param>
public sealed record CreateGradeCommand(
    string Name,
    int Level,
    decimal MinSalary,
    decimal MaxSalary) : IRequest<Result<Guid>>;
