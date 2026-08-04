using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Employee360.Application.Features.Reports;

/// <summary>
/// Unified report query (FR-RPT-001..015 + dashboards).
/// GET /api/v1/reports/{reportId} with fromDate, toDate, year, format query params.
/// </summary>
public sealed record GetReportQuery(
    string ReportId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? Year = null,
    string Format = ReportFormats.Json) : IRequest<Result<ReportResponse>>;

public sealed class GetReportValidator : AbstractValidator<GetReportQuery>
{
    public GetReportValidator()
    {
        RuleFor(q => q.ReportId).NotEmpty();
        RuleFor(q => q.ReportId)
            .Must(ReportIds.IsValid)
            .WithMessage("Unknown report id.");

        RuleFor(q => q.Format)
            .Must(ReportFormats.IsValid)
            .WithMessage("Format must be json, csv, or xlsx.");

        RuleFor(q => q)
            .Must(q => q.FromDate is null || q.ToDate is null || q.FromDate <= q.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");
    }
}

public sealed class GetReportHandler : IRequestHandler<GetReportQuery, Result<ReportResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICsvExporter _csvExporter;
    private readonly IExcelExporter _excelExporter;
    private readonly IDateTimeProvider _clock;

    public GetReportHandler(
        IApplicationDbContext context,
        ICsvExporter csvExporter,
        IExcelExporter excelExporter,
        IDateTimeProvider clock)
    {
        _context = context;
        _csvExporter = csvExporter;
        _excelExporter = excelExporter;
        _clock = clock;
    }

    public async Task<Result<ReportResponse>> Handle(
        GetReportQuery request,
        CancellationToken cancellationToken)
    {
        var reportId = request.ReportId.ToLowerInvariant();
        var today = _clock.TodayWat;
        var year = request.Year ?? today.Year;

        ReportDateRange range;
        try
        {
            range = ReportDateRange.Resolve(request.FromDate, request.ToDate, today);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ReportResponse>(ex.Message);
        }

        var table = reportId switch
        {
            ReportIds.HeadcountSummary =>
                await HeadcountReportGenerator.GenerateSummaryAsync(_context, range, cancellationToken),
            ReportIds.HeadcountTrend =>
                await HeadcountReportGenerator.GenerateTrendAsync(_context, range, cancellationToken),
            ReportIds.Attrition =>
                await AttritionReportGenerator.GenerateAsync(_context, range, cancellationToken),
            ReportIds.LeaveBalance =>
                await LeaveReportGenerator.GenerateBalanceAsync(_context, year, cancellationToken),
            ReportIds.LeaveUtilization =>
                await LeaveReportGenerator.GenerateUtilizationAsync(_context, year, cancellationToken),
            ReportIds.LeaveHistory =>
                await LeaveReportGenerator.GenerateHistoryAsync(_context, range, cancellationToken),
            ReportIds.AttendanceSummary =>
                await AttendanceReportGenerator.GenerateSummaryAsync(_context, range, cancellationToken),
            ReportIds.PayrollRegister =>
                await PayrollReportGenerator.GenerateRegisterAsync(_context, range, cancellationToken),
            ReportIds.PayrollCostByDepartment =>
                await PayrollReportGenerator.GenerateCostByDepartmentAsync(_context, range, cancellationToken),
            ReportIds.PayeSchedule =>
                await PayrollReportGenerator.GeneratePayeScheduleAsync(_context, range, cancellationToken),
            ReportIds.PensionSchedule =>
                await PayrollReportGenerator.GeneratePensionScheduleAsync(_context, range, cancellationToken),
            ReportIds.NhfSchedule =>
                await PayrollReportGenerator.GenerateNhfScheduleAsync(_context, range, cancellationToken),
            ReportIds.EmployeeMasterList =>
                await EmployeeReportGenerator.GenerateMasterListAsync(_context, cancellationToken),
            ReportIds.BirthdayAnniversary =>
                await EmployeeReportGenerator.GenerateBirthdayAnniversaryAsync(_context, range, cancellationToken),
            ReportIds.ExecutiveDashboard =>
                await DashboardReportGenerator.GenerateExecutiveAsync(_context, range, cancellationToken),
            ReportIds.HrDashboard =>
                await DashboardReportGenerator.GenerateHrAsync(_context, range, year, cancellationToken),
            ReportIds.PayrollDashboard =>
                await DashboardReportGenerator.GeneratePayrollAsync(_context, range, cancellationToken),
            _ => null,
        };

        if (table is null)
        {
            return Result.Failure<ReportResponse>("Report not found.");
        }

        var format = request.Format.ToLowerInvariant();
        byte[]? exportBytes = null;
        string? contentType = null;
        string? fileName = null;

        if (format == ReportFormats.Csv)
        {
            exportBytes = _csvExporter.Export(table.Headers, table.Rows);
            contentType = "text/csv";
            fileName = $"{reportId}.csv";
        }
        else if (format == ReportFormats.Excel)
        {
            exportBytes = _excelExporter.Export(table.Title, table.Headers, table.Rows);
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileName = $"{reportId}.xlsx";
        }

        var data = format == ReportFormats.Json
            ? new
            {
                table.Title,
                table.Headers,
                table.Rows,
                table.Summary,
            }
            : null;

        return Result.Success(new ReportResponse(
            reportId,
            table.Title,
            range.FromDate,
            range.ToDate,
            data,
            exportBytes,
            contentType,
            fileName));
    }
}

/// <summary>Lists available report ids for the API catalog endpoint.</summary>
public sealed record ListReportsQuery : IRequest<Result<IReadOnlyList<ReportCatalogItem>>>;

public sealed record ReportCatalogItem(string Id, string Name, string Category);

public sealed class ListReportsHandler : IRequestHandler<ListReportsQuery, Result<IReadOnlyList<ReportCatalogItem>>>
{
    public Task<Result<IReadOnlyList<ReportCatalogItem>>> Handle(
        ListReportsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReportCatalogItem> catalog =
        [
            new(ReportIds.HeadcountSummary, "Headcount Summary", "Workforce"),
            new(ReportIds.HeadcountTrend, "Headcount Trend", "Workforce"),
            new(ReportIds.Attrition, "Attrition", "Workforce"),
            new(ReportIds.LeaveBalance, "Leave Balance", "Leave"),
            new(ReportIds.LeaveUtilization, "Leave Utilization", "Leave"),
            new(ReportIds.LeaveHistory, "Leave History", "Leave"),
            new(ReportIds.AttendanceSummary, "Attendance Summary", "Attendance"),
            new(ReportIds.PayrollRegister, "Payroll Register", "Payroll"),
            new(ReportIds.PayrollCostByDepartment, "Payroll Cost by Department", "Payroll"),
            new(ReportIds.PayeSchedule, "PAYE Schedule", "Payroll"),
            new(ReportIds.PensionSchedule, "Pension Schedule", "Payroll"),
            new(ReportIds.NhfSchedule, "NHF Schedule", "Payroll"),
            new(ReportIds.EmployeeMasterList, "Employee Master List", "Workforce"),
            new(ReportIds.BirthdayAnniversary, "Birthday & Anniversary", "Workforce"),
            new(ReportIds.ExecutiveDashboard, "Executive Dashboard", "Dashboard"),
            new(ReportIds.HrDashboard, "HR Dashboard", "Dashboard"),
            new(ReportIds.PayrollDashboard, "Payroll Dashboard", "Dashboard"),
        ];

        return Task.FromResult(Result.Success(catalog));
    }
}
