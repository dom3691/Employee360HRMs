using Employee360.Application.Common.Services;
using Employee360.Domain.Enums;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Payroll;

/// <summary>
/// Payroll run state machine transitions (FR-PAY-008..011).
/// </summary>
public class PayrollRunWorkflowTests
{
    [Theory]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Calculated, true)]
    [InlineData(PayrollRunStatus.Calculated, PayrollRunStatus.PendingApproval, true)]
    [InlineData(PayrollRunStatus.PendingApproval, PayrollRunStatus.Approved, true)]
    [InlineData(PayrollRunStatus.Approved, PayrollRunStatus.Finalized, true)]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Finalized, false)]
    [InlineData(PayrollRunStatus.Finalized, PayrollRunStatus.Draft, false)]
    [InlineData(PayrollRunStatus.Calculated, PayrollRunStatus.Approved, false)]
    public void EnsureTransition_ValidatesWorkflow(
        PayrollRunStatus from,
        PayrollRunStatus to,
        bool shouldSucceed)
    {
        var result = PayrollRunWorkflow.EnsureTransition(from, to);
        result.IsSuccess.Should().Be(shouldSucceed);
    }

    [Fact]
    public void IsPeriodLocked_OnlyWhenFinalized()
    {
        PayrollRunWorkflow.IsPeriodLocked(PayrollRunStatus.Finalized).Should().BeTrue();
        PayrollRunWorkflow.IsPeriodLocked(PayrollRunStatus.Approved).Should().BeFalse();
    }
}
