using Employee360.Application.Common.Authorization;
using Employee360.Domain.Constants;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Admin;

public class RolePermissionMatrixMapperTests
{
    [Fact]
    public void MergeMatrixPermissions_UnchangedModule_PreservesGranularGrants()
    {
        var existing = new List<string>
        {
            Permissions.Employees.ViewTeam,
            Permissions.Leave.AdjustBalances,
            Permissions.Leave.ApproveTeam,
            Permissions.Leave.ViewTeamCalendar,
        };

        var matrix = RolePermissionMatrixMapper.ToMatrix(existing);

        var merged = RolePermissionMatrixMapper.MergeMatrixPermissions(existing, matrix);

        merged.Should().Contain(Permissions.Employees.ViewTeam);
        merged.Should().Contain(Permissions.Leave.AdjustBalances);
        merged.Should().Contain(Permissions.Leave.ApproveTeam);
        merged.Should().Contain(Permissions.Leave.ViewTeamCalendar);
        merged.Should().NotContain(Permissions.Employees.ViewAll);
    }

    [Fact]
    public void MergeMatrixPermissions_ChangedEmployeesViewOff_RemovesViewGrants()
    {
        var existing = new List<string>
        {
            Permissions.Employees.ViewTeam,
            Permissions.Employees.EditOwnProfile,
        };

        var matrix = RolePermissionMatrixMapper.ToMatrix(existing);
        matrix = new Dictionary<string, RolePermissionMatrixMapper.ModulePermissionFlags>(matrix)
        {
            ["employees"] = new RolePermissionMatrixMapper.ModulePermissionFlags(
                View: false, Create: false, Edit: false, Delete: false, Approve: false),
        };

        var merged = RolePermissionMatrixMapper.MergeMatrixPermissions(existing, matrix);

        merged.Should().Contain(Permissions.Employees.EditOwnProfile);
        merged.Should().NotContain(Permissions.Employees.ViewTeam);
        merged.Should().NotContain(Permissions.Employees.ViewAll);
    }

    [Fact]
    public void MergeMatrixPermissions_ChangedEmployeesViewOn_AddsDefaultViewGrant()
    {
        var existing = new List<string>
        {
            Permissions.Employees.EditOwnProfile,
        };

        var matrix = RolePermissionMatrixMapper.ToMatrix(existing);
        matrix = new Dictionary<string, RolePermissionMatrixMapper.ModulePermissionFlags>(matrix)
        {
            ["employees"] = new RolePermissionMatrixMapper.ModulePermissionFlags(
                View: true, Create: false, Edit: false, Delete: false, Approve: false),
        };

        var merged = RolePermissionMatrixMapper.MergeMatrixPermissions(existing, matrix);

        merged.Should().Contain(Permissions.Employees.ViewAll);
        merged.Should().Contain(Permissions.Employees.EditOwnProfile);
    }
}
