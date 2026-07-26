using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Employees.GetOrgChart;

/// <summary>A node in the organization chart tree (FR-EMP-008).</summary>
public sealed record OrgChartNode(
    Guid EmployeeId,
    string FullName,
    string? PositionTitle,
    string? DepartmentName,
    IReadOnlyList<OrgChartNode> Children);

/// <summary>Builds the org chart from the reporting hierarchy (FR-EMP-008).</summary>
public sealed record GetOrgChartQuery : IRequest<Result<IReadOnlyList<OrgChartNode>>>;
