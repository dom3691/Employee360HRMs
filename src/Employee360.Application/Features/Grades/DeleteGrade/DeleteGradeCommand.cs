using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Grades.DeleteGrade;

/// <summary>Soft-deletes a grade. Blocked while positions reference it.</summary>
/// <param name="GradeId">The grade to delete.</param>
public sealed record DeleteGradeCommand(Guid GradeId) : IRequest<Result>;
