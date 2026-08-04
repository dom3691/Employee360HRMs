using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Payroll.Calculate;
using Employee360.Application.Features.Payroll.Deductions;
using Employee360.Application.Features.Payroll.EmployeeSalaries;
using Employee360.Application.Features.Payroll.Runs;
using Employee360.Application.Features.Payroll.TaxBands;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>
/// Employee salary assignment, pension details, tax config, deductions, and calculation preview.
/// </summary>
[Route("api/v1/payroll")]
public sealed class PayrollController : ApiControllerBase
{
    // --- Employee salary (FR-PAY-002, FR-PAY-013) ---

    [HttpPost("employee-salaries")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignSalary(
        [FromBody] AssignEmployeeSalaryCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpGet("employees/{employeeId:guid}/salary-history")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeSalaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalaryHistory(
        [FromRoute] Guid employeeId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetEmployeeSalaryHistoryQuery(employeeId), cancellationToken));

    [HttpPut("employees/{employeeId:guid}/pension-details")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePensionDetails(
        [FromRoute] Guid employeeId,
        [FromBody] UpdatePensionDetailsRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateEmployeePensionDetailsCommand(employeeId, body.PensionPin, body.PfaName),
            cancellationToken));

    // --- Tax bands (FR-PAY-003) ---

    [HttpGet("tax-bands")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(IReadOnlyList<TaxBandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxBands(
        [FromQuery] int taxYear,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetTaxBandsQuery(taxYear), cancellationToken));

    [HttpPost("tax-bands")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertTaxBand(
        [FromBody] UpsertTaxBandCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpDelete("tax-bands/{id:guid}")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTaxBand([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteTaxBandCommand(id), cancellationToken));

    // --- Statutory rates (FR-PAY-004..006) ---

    [HttpGet("statutory-rates")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(IReadOnlyList<StatutoryRateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatutoryRates(
        [FromQuery] int taxYear,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetStatutoryRatesQuery(taxYear), cancellationToken));

    [HttpPost("statutory-rates")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertStatutoryRate(
        [FromBody] UpsertStatutoryRateCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    // --- Deductions (FR-PAY-007) ---

    [HttpGet("employees/{employeeId:guid}/deductions")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(IReadOnlyList<PayrollDeductionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeductions(
        [FromRoute] Guid employeeId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetPayrollDeductionsQuery(employeeId), cancellationToken));

    [HttpPost("deductions")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDeduction(
        [FromBody] CreatePayrollDeductionCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPut("deductions/{id:guid}")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDeduction(
        [FromRoute] Guid id,
        [FromBody] UpdatePayrollDeductionRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdatePayrollDeductionCommand(
                id, body.Name, body.DeductionType, body.FixedAmount, body.PercentOfGross,
                body.IsRecurring, body.StartDate, body.EndDate, body.IsActive),
            cancellationToken));

    [HttpDelete("deductions/{id:guid}")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDeduction([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeletePayrollDeductionCommand(id), cancellationToken));

    // --- Calculation preview (FR-PAY-003..007, FR-PAY-012) ---

    [HttpPost("calculate/preview")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(PayrollCalculationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewCalculation(
        [FromBody] PreviewPayrollCalculationQuery query,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(query, cancellationToken));

    // --- Payroll runs (FR-PAY-008..011, FR-PAY-014) ---

    [HttpPost("runs")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateRun(
        [FromBody] InitiatePayrollRunRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new InitiatePayrollRunCommand(body.PeriodYear, body.PeriodMonth, body.TaxYear),
            cancellationToken));

    [HttpGet("runs")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(IReadOnlyList<PayrollRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuns(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new ListPayrollRunsQuery(year), cancellationToken));

    [HttpGet("runs/{id:guid}")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(PayrollRunDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRun([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetPayrollRunQuery(id), cancellationToken));

    [HttpPost("runs/{id:guid}/calculate")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateRun([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new CalculatePayrollRunCommand(id), cancellationToken));

    [HttpGet("runs/{id:guid}/review")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(PayrollReviewReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewRun([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new ReviewPayrollRunQuery(id), cancellationToken));

    [HttpPost("runs/{id:guid}/submit")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitRun([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new SubmitPayrollRunForApprovalCommand(id), cancellationToken));

    [HttpPost("runs/{id:guid}/approve")]
    [HasPermission(Permissions.Payroll.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveRun([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new ApprovePayrollRunCommand(id), cancellationToken));

    [HttpPost("runs/{id:guid}/finalize")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FinalizeRun([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new FinalizePayrollRunCommand(id), cancellationToken));

    [HttpGet("runs/{id:guid}/payslips")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(IReadOnlyList<PayslipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRunPayslips([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetPayrollRunPayslipsQuery(id), cancellationToken));

    [HttpGet("payslips/{id:guid}/download")]
    [HasPermission(Permissions.Payroll.ViewOwnPayslips)]
    [ProducesResponseType(typeof(PayslipDownloadDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadPayslip([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DownloadPayslipQuery(id), cancellationToken));

    [HttpGet("runs/{id:guid}/bank-file")]
    [HasPermission(Permissions.Payroll.ExportBankFile)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBankFile([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ExportBankPaymentFileQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value!, "text/csv", $"payroll-bank-file-{id}.csv");
    }

    [HttpGet("runs/{id:guid}/remittance/{scheduleType}")]
    [HasPermission(Permissions.Payroll.Run)]
    [ProducesResponseType(typeof(StatutoryRemittanceReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRemittanceReport(
        [FromRoute] Guid id,
        [FromRoute] string scheduleType,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetStatutoryRemittanceReportQuery(id, scheduleType),
            cancellationToken));

    public sealed record InitiatePayrollRunRequest(int PeriodYear, int PeriodMonth, int? TaxYear);

    public sealed record UpdatePensionDetailsRequest(string? PensionPin, string? PfaName);

    public sealed record UpdatePayrollDeductionRequest(
        string Name,
        Domain.Enums.PayrollDeductionType DeductionType,
        decimal? FixedAmount,
        decimal? PercentOfGross,
        bool IsRecurring,
        DateOnly StartDate,
        DateOnly? EndDate,
        bool IsActive);
}
