using Employee360.Application.Common.Interfaces;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Infrastructure.Identity;

/// <summary>
/// Resolves manager scope by walking the Employee.ManagerId reporting hierarchy
/// (breadth-first). Loads only (Id, ManagerId) pairs in a single query, which is
/// efficient for the PRD's target scale (≤10,000 employees, NFR-SCAL-001).
/// </summary>
public sealed class ManagerScopeService : IManagerScopeService
{
    private readonly Employee360DbContext _context;

    public ManagerScopeService(Employee360DbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetManagedEmployeeIdsAsync(
        Guid managerEmployeeId,
        bool includeIndirect = true,
        CancellationToken cancellationToken = default)
    {
        if (!includeIndirect)
        {
            return await _context.Employees
                .AsNoTracking()
                .Where(e => e.ManagerId == managerEmployeeId)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
        }

        // Single round-trip for the full reporting graph, then a BFS in memory.
        var reportPairs = await _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId != null)
            .Select(e => new { e.Id, ManagerId = e.ManagerId!.Value })
            .ToListAsync(cancellationToken);

        var reportsByManager = reportPairs
            .GroupBy(p => p.ManagerId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

        var managed = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(managerEmployeeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!reportsByManager.TryGetValue(current, out var directReports))
            {
                continue;
            }

            foreach (var reportId in directReports)
            {
                // HashSet.Add guards against cycles in corrupt hierarchies.
                if (managed.Add(reportId))
                {
                    queue.Enqueue(reportId);
                }
            }
        }

        return managed;
    }

    /// <inheritdoc />
    public async Task<bool> IsManagerOfAsync(
        Guid managerEmployeeId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var managed = await GetManagedEmployeeIdsAsync(
            managerEmployeeId,
            includeIndirect: true,
            cancellationToken);

        return managed.Contains(employeeId);
    }
}
