using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Services;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Employee360.Application.Features.Payroll.Runs;

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

public sealed record PayrollRunDto(
    Guid Id,
    int PeriodYear,
    int PeriodMonth,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int TaxYear,
    PayrollRunStatus Status,
    int TotalEmployees,
    int ProcessedEmployees,
    string? LastError,
    string? CalculationJobId,
    DateTime? SubmittedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? FinalizedAtUtc);

public sealed record PayslipDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal GrossPay,
    decimal Paye,
    decimal PensionEmployee,
    decimal Nhf,
    decimal NsitfEmployer,
    decimal NetPay,
    string? PdfPath);

public sealed record PayrollReviewLineDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal CurrentNetPay,
    decimal? PriorNetPay,
    decimal Variance,
    decimal VariancePercent,
    bool IsException);

public sealed record PayrollReviewReportDto(
    Guid PayrollRunId,
    string PeriodLabel,
    decimal TotalNetPay,
    decimal? PriorTotalNetPay,
    IReadOnlyList<PayrollReviewLineDto> Lines,
    IReadOnlyList<PayrollReviewLineDto> Exceptions);

public sealed record StatutoryRemittanceLineDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal Amount);

public sealed record StatutoryRemittanceReportDto(
    string ScheduleType,
    string PeriodLabel,
    decimal TotalAmount,
    IReadOnlyList<StatutoryRemittanceLineDto> Lines);

public sealed record PayslipDownloadDto(string DownloadUrl);

// ---------------------------------------------------------------------------
// Initiate (Draft)
// ---------------------------------------------------------------------------

public sealed record InitiatePayrollRunCommand(int PeriodYear, int PeriodMonth, int? TaxYear)
    : IRequest<Result<Guid>>;

public sealed class InitiatePayrollRunValidator : AbstractValidator<InitiatePayrollRunCommand>
{
    public InitiatePayrollRunValidator()
    {
        RuleFor(c => c.PeriodYear).GreaterThan(2000);
        RuleFor(c => c.PeriodMonth).InclusiveBetween(1, 12);
    }
}

public sealed class InitiatePayrollRunHandler : IRequestHandler<InitiatePayrollRunCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InitiatePayrollRunHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(InitiatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var periodStart = new DateOnly(request.PeriodYear, request.PeriodMonth, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var finalizedExists = await _context.PayrollRuns
            .AnyAsync(r =>
                r.PeriodYear == request.PeriodYear &&
                r.PeriodMonth == request.PeriodMonth &&
                r.Status == PayrollRunStatus.Finalized,
                cancellationToken);

        if (finalizedExists)
        {
            return Result.Failure<Guid>("This pay period is finalized and locked.");
        }

        var openRun = await _context.PayrollRuns
            .FirstOrDefaultAsync(r =>
                r.PeriodYear == request.PeriodYear &&
                r.PeriodMonth == request.PeriodMonth &&
                r.Status != PayrollRunStatus.Finalized,
                cancellationToken);

        if (openRun is not null)
        {
            return Result.Success(openRun.Id);
        }

        var run = new PayrollRun
        {
            PeriodYear = request.PeriodYear,
            PeriodMonth = request.PeriodMonth,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PeriodLabel = PayrollRunWorkflow.BuildPeriodLabel(request.PeriodYear, request.PeriodMonth),
            TaxYear = request.TaxYear ?? request.PeriodYear,
            Status = PayrollRunStatus.Draft,
            RunByUserId = _currentUser.UserId,
        };

        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(run.Id);
    }
}

// ---------------------------------------------------------------------------
// Calculate (enqueue Hangfire job)
// ---------------------------------------------------------------------------

public sealed record CalculatePayrollRunCommand(Guid PayrollRunId) : IRequest<Result<string?>>;

public sealed class CalculatePayrollRunValidator : AbstractValidator<CalculatePayrollRunCommand>
{
    public CalculatePayrollRunValidator()
    {
        RuleFor(c => c.PayrollRunId).NotEmpty();
    }
}

public sealed class CalculatePayrollRunHandler : IRequestHandler<CalculatePayrollRunCommand, Result<string?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollCalculationJobScheduler _scheduler;

    public CalculatePayrollRunHandler(
        IApplicationDbContext context,
        IPayrollCalculationJobScheduler scheduler)
    {
        _context = context;
        _scheduler = scheduler;
    }

    public async Task<Result<string?>> Handle(
        CalculatePayrollRunCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure<string?>("Payroll run not found.");
        }

        if (run.Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated))
        {
            return Result.Failure<string?>(
                $"Calculation is only allowed when status is Draft or Calculated (current: {run.Status}).");
        }

        if (PayrollRunWorkflow.IsPeriodLocked(run.Status))
        {
            return Result.Failure<string?>("Pay period is locked.");
        }

        var jobId = await _scheduler.ScheduleCalculationAsync(run.Id, cancellationToken);
        run.CalculationJobId = jobId;
        run.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(jobId);
    }
}

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

public sealed record GetPayrollRunQuery(Guid PayrollRunId) : IRequest<Result<PayrollRunDto>>;

public sealed class GetPayrollRunHandler : IRequestHandler<GetPayrollRunQuery, Result<PayrollRunDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollRunHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PayrollRunDto>> Handle(
        GetPayrollRunQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        return run is null
            ? Result.Failure<PayrollRunDto>("Payroll run not found.")
            : Result.Success(MapRun(run));
    }

    internal static PayrollRunDto MapRun(PayrollRun run) =>
        new(run.Id, run.PeriodYear, run.PeriodMonth, run.PeriodLabel,
            run.PeriodStart, run.PeriodEnd, run.TaxYear, run.Status,
            run.TotalEmployees, run.ProcessedEmployees, run.LastError,
            run.CalculationJobId, run.SubmittedAtUtc, run.ApprovedAtUtc, run.FinalizedAtUtc);
}

public sealed record ListPayrollRunsQuery(
    int Page = 1,
    int PageSize = 20,
    int? Year = null) : IRequest<Result<PagedResult<PayrollRunDto>>>;

public sealed class ListPayrollRunsValidator : AbstractValidator<ListPayrollRunsQuery>
{
    public ListPayrollRunsValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

public sealed class ListPayrollRunsHandler : IRequestHandler<ListPayrollRunsQuery, Result<PagedResult<PayrollRunDto>>>
{
    private readonly IApplicationDbContext _context;

    public ListPayrollRunsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<PayrollRunDto>>> Handle(
        ListPayrollRunsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PayrollRuns.AsNoTracking();

        if (request.Year.HasValue)
        {
            query = query.Where(r => r.PeriodYear == request.Year.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var runs = await query
            .OrderByDescending(r => r.PeriodYear)
            .ThenByDescending(r => r.PeriodMonth)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = runs.Select(GetPayrollRunHandler.MapRun).ToList();

        return Result.Success(new PagedResult<PayrollRunDto>(
            items, request.Page, request.PageSize, total));
    }
}

public sealed record GetPayrollRunPayslipsQuery(Guid PayrollRunId)
    : IRequest<Result<IReadOnlyList<PayslipDto>>>;

public sealed class GetPayrollRunPayslipsHandler
    : IRequestHandler<GetPayrollRunPayslipsQuery, Result<IReadOnlyList<PayslipDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollRunPayslipsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<PayslipDto>>> Handle(
        GetPayrollRunPayslipsQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await _context.PayrollRuns
            .AnyAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (!exists)
        {
            return Result.Failure<IReadOnlyList<PayslipDto>>("Payroll run not found.");
        }

        var payslips = await _context.Payslips
            .AsNoTracking()
            .Where(p => p.PayrollRunId == request.PayrollRunId)
            .OrderBy(p => p.EmployeeName)
            .Select(p => new PayslipDto(
                p.Id, p.EmployeeId, p.EmployeeCode, p.EmployeeName,
                p.GrossPay, p.Paye, p.PensionEmployee, p.Nhf, p.NsitfEmployer,
                p.NetPay, p.PdfPath))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PayslipDto>>(payslips);
    }
}

// ---------------------------------------------------------------------------
// Review (variance vs prior period)
// ---------------------------------------------------------------------------

public sealed record ReviewPayrollRunQuery(Guid PayrollRunId)
    : IRequest<Result<PayrollReviewReportDto>>;

public sealed class ReviewPayrollRunHandler : IRequestHandler<ReviewPayrollRunQuery, Result<PayrollReviewReportDto>>
{
    private readonly IApplicationDbContext _context;

    public ReviewPayrollRunHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PayrollReviewReportDto>> Handle(
        ReviewPayrollRunQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure<PayrollReviewReportDto>("Payroll run not found.");
        }

        if (run.Status == PayrollRunStatus.Draft)
        {
            return Result.Failure<PayrollReviewReportDto>(
                "Review is available after calculation completes.");
        }

        var current = await _context.Payslips
            .AsNoTracking()
            .Where(p => p.PayrollRunId == run.Id)
            .ToListAsync(cancellationToken);

        var priorRun = await _context.PayrollRuns
            .AsNoTracking()
            .Where(r =>
                r.Status == PayrollRunStatus.Finalized &&
                (r.PeriodYear < run.PeriodYear ||
                 (r.PeriodYear == run.PeriodYear && r.PeriodMonth < run.PeriodMonth)))
            .OrderByDescending(r => r.PeriodYear)
            .ThenByDescending(r => r.PeriodMonth)
            .FirstOrDefaultAsync(cancellationToken);

        Dictionary<Guid, decimal>? priorByEmployee = null;

        if (priorRun is not null)
        {
            priorByEmployee = await _context.Payslips
                .AsNoTracking()
                .Where(p => p.PayrollRunId == priorRun.Id)
                .ToDictionaryAsync(p => p.EmployeeId, p => p.NetPay, cancellationToken);
        }

        var lines = current.Select(p =>
        {
            decimal? priorNet = null;
            if (priorByEmployee?.TryGetValue(p.EmployeeId, out var prior) == true)
            {
                priorNet = prior;
            }

            var variance = priorNet.HasValue ? p.NetPay - priorNet.Value : p.NetPay;
            var variancePct = priorNet is > 0
                ? Math.Round(variance / priorNet.Value * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
            var isException = priorNet.HasValue &&
                              Math.Abs(variancePct) >= 10m;

            return new PayrollReviewLineDto(
                p.EmployeeId, p.EmployeeCode, p.EmployeeName,
                p.NetPay, priorNet, variance, variancePct, isException);
        }).ToList();

        var priorTotal = priorByEmployee?.Values.Sum();

        return Result.Success(new PayrollReviewReportDto(
            run.Id,
            run.PeriodLabel,
            current.Sum(p => p.NetPay),
            priorTotal,
            lines,
            lines.Where(l => l.IsException).ToList()));
    }
}

// ---------------------------------------------------------------------------
// Submit for approval (EML-008)
// ---------------------------------------------------------------------------

public sealed record SubmitPayrollRunForApprovalCommand(Guid PayrollRunId) : IRequest<Result>;

public sealed class SubmitPayrollRunForApprovalHandler : IRequestHandler<SubmitPayrollRunForApprovalCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IDateTimeProvider _clock;

    public SubmitPayrollRunForApprovalHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IDateTimeProvider clock)
    {
        _context = context;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _clock = clock;
    }

    public async Task<Result> Handle(
        SubmitPayrollRunForApprovalCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure("Payroll run not found.");
        }

        var transition = PayrollRunWorkflow.EnsureTransition(run.Status, PayrollRunStatus.PendingApproval);
        if (transition.IsFailure)
        {
            return transition;
        }

        if (!await _context.Payslips.AnyAsync(p => p.PayrollRunId == run.Id, cancellationToken))
        {
            return Result.Failure("Cannot submit an empty payroll run.");
        }

        run.Status = PayrollRunStatus.PendingApproval;
        run.SubmittedAtUtc = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await NotifyApproversAsync(run, cancellationToken);

        return Result.Success();
    }

    private async Task NotifyApproversAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var approverEmails = await (
            from ur in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on ur.RoleId equals role.Id
            join user in _context.Users.AsNoTracking() on ur.UserId equals user.Id
            where role.Name == RoleNames.HRManager || role.Name == RoleNames.PayrollOfficer
            select user.Email)
            .Distinct()
            .ToListAsync(cancellationToken);

        var tokens = new Dictionary<string, string> { ["Period"] = run.PeriodLabel };
        var rendered = await _emailTemplateService.RenderAsync(
            EmailTemplateCodes.PayrollPendingApproval, tokens, cancellationToken);

        foreach (var email in approverEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            await _emailService.SendAsync(email!, rendered.Subject, rendered.BodyHtml, cancellationToken);
        }
    }
}

// ---------------------------------------------------------------------------
// Approve (HR Manager)
// ---------------------------------------------------------------------------

public sealed record ApprovePayrollRunCommand(Guid PayrollRunId) : IRequest<Result>;

public sealed class ApprovePayrollRunHandler : IRequestHandler<ApprovePayrollRunCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ApprovePayrollRunHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(ApprovePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure("Payroll run not found.");
        }

        var transition = PayrollRunWorkflow.EnsureTransition(run.Status, PayrollRunStatus.Approved);
        if (transition.IsFailure)
        {
            return transition;
        }

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedByUserId = _currentUser.UserId;
        run.ApprovedAtUtc = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// Finalize (PDFs, lock period, EML-007)
// ---------------------------------------------------------------------------

public sealed record FinalizePayrollRunCommand(Guid PayrollRunId) : IRequest<Result>;

public sealed class FinalizePayrollRunHandler : IRequestHandler<FinalizePayrollRunCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayslipPdfGenerator _pdfGenerator;
    private readonly IPayslipStorageService _payslipStorage;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IDateTimeProvider _clock;

    public FinalizePayrollRunHandler(
        IApplicationDbContext context,
        IPayslipPdfGenerator pdfGenerator,
        IPayslipStorageService payslipStorage,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IDateTimeProvider clock)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _payslipStorage = payslipStorage;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _clock = clock;
    }

    public async Task<Result> Handle(FinalizePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Payslips)
            .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure("Payroll run not found.");
        }

        var transition = PayrollRunWorkflow.EnsureTransition(run.Status, PayrollRunStatus.Finalized);
        if (transition.IsFailure)
        {
            return transition;
        }

        var companyName = await _context.Companies
            .AsNoTracking()
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "Employee360";

        var generatedAtWat = _clock.UtcNow.AddHours(1);

        foreach (var payslip in run.Payslips)
        {
            var customDeductions = DeserializeCustomDeductions(payslip.CustomDeductionsJson);

            var pdf = _pdfGenerator.Generate(new PayslipPdfModel(
                companyName,
                run.PeriodLabel,
                payslip.EmployeeCode,
                payslip.EmployeeName,
                payslip.Basic,
                payslip.Housing,
                payslip.Transport,
                payslip.OtherAllowances,
                payslip.GrossPay,
                payslip.PayFactor,
                payslip.CraMonthly,
                payslip.Paye,
                payslip.PensionEmployee,
                payslip.PensionEmployer,
                payslip.Nhf,
                payslip.NsitfEmployer,
                payslip.CustomDeductionsTotal,
                payslip.NetPay,
                customDeductions,
                generatedAtWat));

            var path = $"{run.PeriodYear}/{run.PeriodMonth:D2}/{payslip.EmployeeId}.pdf";
            await _payslipStorage.UploadAsync(path, pdf, cancellationToken);
            payslip.PdfPath = path;

            if (!string.IsNullOrWhiteSpace(payslip.Employee.Email))
            {
                var tokens = new Dictionary<string, string> { ["Period"] = run.PeriodLabel };
                var rendered = await _emailTemplateService.RenderAsync(
                    EmailTemplateCodes.PayslipAvailable, tokens, cancellationToken);

                await _emailService.SendAsync(
                    payslip.Employee.Email, rendered.Subject, rendered.BodyHtml, cancellationToken);
            }
        }

        run.Status = PayrollRunStatus.Finalized;
        run.FinalizedAtUtc = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static IReadOnlyList<PayrollCustomDeductionInput> DeserializeCustomDeductions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<PayrollCustomDeductionInput>();
        }

        return JsonSerializer.Deserialize<List<PayrollCustomDeductionInput>>(json)
               ?? (IReadOnlyList<PayrollCustomDeductionInput>)Array.Empty<PayrollCustomDeductionInput>();
    }
}

// ---------------------------------------------------------------------------
// Payslip download
// ---------------------------------------------------------------------------

public sealed record DownloadPayslipQuery(Guid PayslipId) : IRequest<Result<PayslipDownloadDto>>;

public sealed class DownloadPayslipHandler : IRequestHandler<DownloadPayslipQuery, Result<PayslipDownloadDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayslipStorageService _payslipStorage;
    private readonly ICurrentUserService _currentUser;

    public DownloadPayslipHandler(
        IApplicationDbContext context,
        IPayslipStorageService payslipStorage,
        ICurrentUserService currentUser)
    {
        _context = context;
        _payslipStorage = payslipStorage;
        _currentUser = currentUser;
    }

    public async Task<Result<PayslipDownloadDto>> Handle(
        DownloadPayslipQuery request,
        CancellationToken cancellationToken)
    {
        var payslip = await _context.Payslips
            .AsNoTracking()
            .Include(p => p.PayrollRun)
            .FirstOrDefaultAsync(p => p.Id == request.PayslipId, cancellationToken);

        if (payslip is null)
        {
            return Result.Failure<PayslipDownloadDto>("Payslip not found.");
        }

        if (payslip.PayrollRun.Status != PayrollRunStatus.Finalized)
        {
            return Result.Failure<PayslipDownloadDto>("Payslip is not yet available for download.");
        }

        if (string.IsNullOrWhiteSpace(payslip.PdfPath))
        {
            return Result.Failure<PayslipDownloadDto>("Payslip PDF has not been generated.");
        }

        var isOwn = _currentUser.EmployeeId == payslip.EmployeeId;
        var canViewAll = _currentUser.Roles.Any(r =>
            r is RoleNames.HRManager or RoleNames.PayrollOfficer or RoleNames.HRAdmin);

        if (!isOwn && !canViewAll)
        {
            return Result.Failure<PayslipDownloadDto>("You are not authorized to download this payslip.");
        }

        var url = await _payslipStorage.GetDownloadUrlAsync(payslip.PdfPath, TimeSpan.FromHours(1), cancellationToken);
        return Result.Success(new PayslipDownloadDto(url));
    }
}

// ---------------------------------------------------------------------------
// Bank file export
// ---------------------------------------------------------------------------

public sealed record ExportBankPaymentFileQuery(Guid PayrollRunId) : IRequest<Result<byte[]>>;

public sealed class ExportBankPaymentFileHandler : IRequestHandler<ExportBankPaymentFileQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryption;
    private readonly IBankPaymentFileExporter _exporter;
    private readonly IOptions<PayrollSettings> _settings;

    public ExportBankPaymentFileHandler(
        IApplicationDbContext context,
        IEncryptionService encryption,
        IBankPaymentFileExporter exporter,
        IOptions<PayrollSettings> settings)
    {
        _context = context;
        _encryption = encryption;
        _exporter = exporter;
        _settings = settings;
    }

    public async Task<Result<byte[]>> Handle(
        ExportBankPaymentFileQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure<byte[]>("Payroll run not found.");
        }

        if (run.Status is not (PayrollRunStatus.Approved or PayrollRunStatus.Finalized))
        {
            return Result.Failure<byte[]>("Bank file export requires an approved payroll run.");
        }

        var payslips = await _context.Payslips
            .AsNoTracking()
            .Where(p => p.PayrollRunId == run.Id)
            .ToListAsync(cancellationToken);

        var employeeIds = payslips.Select(p => p.EmployeeId).ToList();

        var bankAccounts = await _context.EmployeeBankAccounts
            .AsNoTracking()
            .Where(b => employeeIds.Contains(b.EmployeeId))
            .ToDictionaryAsync(b => b.EmployeeId, cancellationToken);

        var narration = _settings.Value.BankFileNarrationFormat
            .Replace("{Period}", run.PeriodLabel, StringComparison.OrdinalIgnoreCase);

        var lines = new List<BankPaymentLine>();

        foreach (var payslip in payslips)
        {
            if (!bankAccounts.TryGetValue(payslip.EmployeeId, out var bank))
            {
                return Result.Failure<byte[]>($"Missing bank account for employee {payslip.EmployeeCode}.");
            }

            var accountNumber = _encryption.Decrypt(bank.AccountNumberEncrypted);

            lines.Add(new BankPaymentLine(
                accountNumber,
                bank.AccountName,
                bank.BankCode,
                payslip.NetPay,
                $"{run.PeriodLabel}-{payslip.EmployeeCode}"));
        }

        var file = _exporter.Export(new BankPaymentFileRequest(
            run.PeriodLabel,
            narration,
            lines,
            _settings.Value.BankFileTemplate));

        return Result.Success(file);
    }
}

// ---------------------------------------------------------------------------
// Statutory remittance reports (Should)
// ---------------------------------------------------------------------------

public sealed record GetStatutoryRemittanceReportQuery(
    Guid PayrollRunId,
    string ScheduleType) : IRequest<Result<StatutoryRemittanceReportDto>>;

public sealed class GetStatutoryRemittanceReportHandler
    : IRequestHandler<GetStatutoryRemittanceReportQuery, Result<StatutoryRemittanceReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStatutoryRemittanceReportHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<StatutoryRemittanceReportDto>> Handle(
        GetStatutoryRemittanceReportQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, cancellationToken);

        if (run is null)
        {
            return Result.Failure<StatutoryRemittanceReportDto>("Payroll run not found.");
        }

        if (run.Status == PayrollRunStatus.Draft)
        {
            return Result.Failure<StatutoryRemittanceReportDto>(
                "Remittance report is available after calculation.");
        }

        var payslips = await _context.Payslips
            .AsNoTracking()
            .Where(p => p.PayrollRunId == run.Id)
            .OrderBy(p => p.EmployeeName)
            .ToListAsync(cancellationToken);

        var schedule = request.ScheduleType.Trim().ToUpperInvariant();

        var lines = schedule switch
        {
            "PAYE" => payslips.Select(p => new StatutoryRemittanceLineDto(
                p.EmployeeId, p.EmployeeCode, p.EmployeeName, p.Paye)).ToList(),
            "PENSION" => payslips.Select(p => new StatutoryRemittanceLineDto(
                p.EmployeeId, p.EmployeeCode, p.EmployeeName,
                p.PensionEmployee + p.PensionEmployer)).ToList(),
            "NHF" => payslips.Select(p => new StatutoryRemittanceLineDto(
                p.EmployeeId, p.EmployeeCode, p.EmployeeName, p.Nhf)).ToList(),
            _ => null,
        };

        if (lines is null)
        {
            return Result.Failure<StatutoryRemittanceReportDto>(
                "ScheduleType must be PAYE, PENSION, or NHF.");
        }

        return Result.Success(new StatutoryRemittanceReportDto(
            schedule,
            run.PeriodLabel,
            lines.Sum(l => l.Amount),
            lines));
    }
}
