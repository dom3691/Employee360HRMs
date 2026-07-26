using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.GetOrgChart;

/// <summary>
/// Handles <see cref="GetOrgChartQuery"/>: loads active employees in one query
/// and assembles the reporting tree in memory. Employees whose manager is missing
/// (or inactive) surface as roots so nobody disappears from the chart.
/// </summary>
public sealed class GetOrgChartHandler
    : IRequestHandler<GetOrgChartQuery, Result<IReadOnlyList<OrgChartNode>>>
{
    private readonly IApplicationDbContext _context;

    public GetOrgChartHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrgChartNode>>> Handle(
        GetOrgChartQuery request,
        CancellationToken cancellationToken)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.ManagerId,
                PositionTitle = e.Position != null ? e.Position.Title : null,
                DepartmentName = e.Department != null ? e.Department.Name : null,
            })
            .ToListAsync(cancellationToken);

        var byId = employees.ToDictionary(e => e.Id);
        var childrenByManager = employees
            .Where(e => e.ManagerId.HasValue)
            .GroupBy(e => e.ManagerId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        OrgChartNode BuildNode(Guid id)
        {
            var employee = byId[id];

            var children = childrenByManager.TryGetValue(id, out var reports)
                ? reports.Select(r => BuildNode(r.Id)).ToList()
                : [];

            return new OrgChartNode(
                employee.Id,
                $"{employee.FirstName} {employee.LastName}",
                employee.PositionTitle,
                employee.DepartmentName,
                children);
        }

        // Roots: no manager, or manager not present in the active set.
        var roots = employees
            .Where(e => e.ManagerId is null || !byId.ContainsKey(e.ManagerId.Value))
            .Select(e => BuildNode(e.Id))
            .ToList();

        return Result.Success<IReadOnlyList<OrgChartNode>>(roots);
    }
}
