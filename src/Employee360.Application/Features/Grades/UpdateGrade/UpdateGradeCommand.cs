using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.UpdateGrade;

/// <summary>Updates a salary grade.</summary>
public sealed record UpdateGradeCommand(
    Guid GradeId,
    string Name,
    int Level,
    decimal MinSalary,
    decimal MaxSalary) : IRequest<Result>;
