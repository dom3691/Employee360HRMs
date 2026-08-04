using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent demo dataset: departments, positions, sample employees/users,
/// leave types, and Nigeria public holidays (Batch 17).
/// Enabled via <c>DemoData:Enabled</c> configuration.
/// </summary>
public sealed class DemoDataSeeder : IDataSeeder
{
    // Fixed ids for idempotent re-runs.
    private static readonly Guid DeptHrId = Guid.Parse("11111111-1111-1111-1111-111111110001");
    private static readonly Guid DeptEngId = Guid.Parse("11111111-1111-1111-1111-111111110002");
    private static readonly Guid DeptFinId = Guid.Parse("11111111-1111-1111-1111-111111110003");

    private static readonly Guid PosHrManagerId = Guid.Parse("22222222-2222-2222-2222-222222220001");
    private static readonly Guid PosEngineerId = Guid.Parse("22222222-2222-2222-2222-222222220002");
    private static readonly Guid PosAccountantId = Guid.Parse("22222222-2222-2222-2222-222222220003");

    private static readonly Guid EmpAdminId = Guid.Parse("33333333-3333-3333-3333-333333330001");
    private static readonly Guid EmpHrAdminId = Guid.Parse("33333333-3333-3333-3333-333333330002");
    private static readonly Guid EmpManagerId = Guid.Parse("33333333-3333-3333-3333-333333330003");
    private static readonly Guid EmpStaff1Id = Guid.Parse("33333333-3333-3333-3333-333333330004");
    private static readonly Guid EmpStaff2Id = Guid.Parse("33333333-3333-3333-3333-333333330005");

    private static readonly Guid UserAdminId = Guid.Parse("44444444-4444-4444-4444-444444440001");
    private static readonly Guid UserHrAdminId = Guid.Parse("44444444-4444-4444-4444-444444440002");
    private static readonly Guid UserManagerId = Guid.Parse("44444444-4444-4444-4444-444444440003");
    private static readonly Guid UserStaff1Id = Guid.Parse("44444444-4444-4444-4444-444444440004");
    private static readonly Guid UserStaff2Id = Guid.Parse("44444444-4444-4444-4444-444444440005");

    private static readonly Guid LeaveAnnualId = Guid.Parse("55555555-5555-5555-5555-555555550001");
    private static readonly Guid LeaveSickId = Guid.Parse("55555555-5555-5555-5555-555555550002");
    private static readonly Guid LeaveMaternityId = Guid.Parse("55555555-5555-5555-5555-555555550003");
    private static readonly Guid LeaveUnpaidId = Guid.Parse("55555555-5555-5555-5555-555555550004");

    private readonly Employee360DbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        Employee360DbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("DemoData:Enabled", defaultValue: false))
        {
            return;
        }

        await SeedDepartmentsAsync(cancellationToken);
        await SeedPositionsAsync(cancellationToken);
        await SeedEmployeesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedLeaveTypesAsync(cancellationToken);
        await SeedPublicHolidaysAsync(cancellationToken);

        _logger.LogInformation("Demo data seed completed (departments, employees, leave types, holidays)");
    }

    private async Task SeedDepartmentsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Departments.AnyAsync(d => d.Id == DeptHrId, cancellationToken))
        {
            return;
        }

        _context.Departments.AddRange(
            new Department { Id = DeptHrId, Name = "Human Resources", Code = "HR" },
            new Department { Id = DeptEngId, Name = "Engineering", Code = "ENG" },
            new Department { Id = DeptFinId, Name = "Finance", Code = "FIN" });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPositionsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Positions.AnyAsync(p => p.Id == PosHrManagerId, cancellationToken))
        {
            return;
        }

        _context.Positions.AddRange(
            new Position
            {
                Id = PosHrManagerId,
                Title = "HR Manager",
                Code = "HR-MGR",
                DepartmentId = DeptHrId,
            },
            new Position
            {
                Id = PosEngineerId,
                Title = "Software Engineer",
                Code = "SW-ENG",
                DepartmentId = DeptEngId,
            },
            new Position
            {
                Id = PosAccountantId,
                Title = "Accountant",
                Code = "ACC",
                DepartmentId = DeptFinId,
            });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEmployeesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Employees.AnyAsync(e => e.Id == EmpAdminId, cancellationToken))
        {
            return;
        }

        var joinDate = new DateOnly(2024, 1, 15);

        _context.Employees.AddRange(
            new Employee
            {
                Id = EmpAdminId,
                EmployeeCode = "EMP-00001",
                FirstName = "Ada",
                LastName = "Okonkwo",
                Email = "admin@employee360.demo",
                DepartmentId = DeptHrId,
                PositionId = PosHrManagerId,
                EmploymentType = EmploymentType.FullTime,
                Status = EmployeeStatus.Active,
                JoinDate = joinDate,
                WorkLocation = "Lagos HQ",
            },
            new Employee
            {
                Id = EmpHrAdminId,
                EmployeeCode = "EMP-00002",
                FirstName = "Chidi",
                LastName = "Eze",
                Email = "hr.admin@employee360.demo",
                DepartmentId = DeptHrId,
                PositionId = PosHrManagerId,
                ManagerId = EmpAdminId,
                EmploymentType = EmploymentType.FullTime,
                Status = EmployeeStatus.Active,
                JoinDate = joinDate.AddMonths(1),
                WorkLocation = "Lagos HQ",
            },
            new Employee
            {
                Id = EmpManagerId,
                EmployeeCode = "EMP-00003",
                FirstName = "Funke",
                LastName = "Adeyemi",
                Email = "manager@employee360.demo",
                DepartmentId = DeptEngId,
                PositionId = PosEngineerId,
                ManagerId = EmpAdminId,
                EmploymentType = EmploymentType.FullTime,
                Status = EmployeeStatus.Active,
                JoinDate = joinDate.AddMonths(2),
                WorkLocation = "Hybrid",
            },
            new Employee
            {
                Id = EmpStaff1Id,
                EmployeeCode = "EMP-00004",
                FirstName = "Tunde",
                LastName = "Balogun",
                Email = "employee1@employee360.demo",
                DepartmentId = DeptEngId,
                PositionId = PosEngineerId,
                ManagerId = EmpManagerId,
                EmploymentType = EmploymentType.FullTime,
                Status = EmployeeStatus.Active,
                JoinDate = joinDate.AddMonths(3),
                WorkLocation = "Remote",
            },
            new Employee
            {
                Id = EmpStaff2Id,
                EmployeeCode = "EMP-00005",
                FirstName = "Ngozi",
                LastName = "Okafor",
                Email = "employee2@employee360.demo",
                DepartmentId = DeptFinId,
                PositionId = PosAccountantId,
                ManagerId = EmpAdminId,
                EmploymentType = EmploymentType.FullTime,
                Status = EmployeeStatus.Active,
                JoinDate = joinDate.AddMonths(4),
                WorkLocation = "Lagos HQ",
            });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Id == UserAdminId, cancellationToken))
        {
            return;
        }

        var password = _configuration.GetValue<string>("DemoData:DefaultPassword") ?? "Demo@12345";
        var hash = _passwordHasher.Hash(password);

        var roles = await _context.Roles
            .Where(r => RoleNames.All.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);

        var users = new[]
        {
            (UserAdminId, EmpAdminId, "admin@employee360.demo", RoleNames.SystemAdmin),
            (UserHrAdminId, EmpHrAdminId, "hr.admin@employee360.demo", RoleNames.HRAdmin),
            (UserManagerId, EmpManagerId, "manager@employee360.demo", RoleNames.LineManager),
            (UserStaff1Id, EmpStaff1Id, "employee1@employee360.demo", RoleNames.Employee),
            (UserStaff2Id, EmpStaff2Id, "employee2@employee360.demo", RoleNames.Employee),
        };

        foreach (var (userId, employeeId, email, roleName) in users)
        {
            _context.Users.Add(new User
            {
                Id = userId,
                Email = email,
                PasswordHash = hash,
                EmployeeId = employeeId,
                IsActive = true,
            });

            if (roles.TryGetValue(roleName, out var roleId))
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Demo users created with password from DemoData:DefaultPassword (default: Demo@12345)");
    }

    private async Task SeedLeaveTypesAsync(CancellationToken cancellationToken)
    {
        if (await _context.LeaveTypes.AnyAsync(lt => lt.Id == LeaveAnnualId, cancellationToken))
        {
            return;
        }

        _context.LeaveTypes.AddRange(
            CreateLeaveType(LeaveAnnualId, "Annual Leave", "ANN", true, "#2E7D32", 21m),
            CreateLeaveType(LeaveSickId, "Sick Leave", "SICK", true, "#C62828", 10m, requiresAttachment: true),
            CreateLeaveType(LeaveMaternityId, "Maternity Leave", "MAT", true, "#6A1B9A", 90m),
            CreateLeaveType(LeaveUnpaidId, "Unpaid Leave", "UNPD", false, "#757575", 0m));

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static LeaveType CreateLeaveType(
        Guid id,
        string name,
        string code,
        bool isPaid,
        string color,
        decimal entitlement,
        bool requiresAttachment = false)
    {
        return new LeaveType
        {
            Id = id,
            Name = name,
            Code = code,
            IsPaid = isPaid,
            RequiresAttachment = requiresAttachment,
            Color = color,
            IsActive = true,
            Policy = new LeavePolicy
            {
                AnnualEntitlement = entitlement,
                AccrualFrequency = AccrualFrequency.Annual,
                CarryForwardMax = isPaid && entitlement > 0 ? 5m : 0m,
                ProbationMonths = 3,
            },
        };
    }

    private async Task SeedPublicHolidaysAsync(CancellationToken cancellationToken)
    {
        var year = _configuration.GetValue("DemoData:HolidayYear", DateTime.UtcNow.Year);

        if (await _context.PublicHolidays.AnyAsync(h => h.Year == year, cancellationToken))
        {
            return;
        }

        var holidays = new (DateOnly Date, string Name)[]
        {
            (new DateOnly(year, 1, 1), "New Year's Day"),
            (new DateOnly(year, 5, 1), "Workers' Day"),
            (new DateOnly(year, 6, 12), "Democracy Day"),
            (new DateOnly(year, 10, 1), "Independence Day"),
            (new DateOnly(year, 12, 25), "Christmas Day"),
            (new DateOnly(year, 12, 26), "Boxing Day"),
        };

        foreach (var (date, name) in holidays)
        {
            _context.PublicHolidays.Add(new PublicHoliday
            {
                Date = date,
                Name = name,
                Year = year,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
