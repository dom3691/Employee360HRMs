using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("Payslips");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.EmployeeCode).HasMaxLength(32).IsRequired();
        builder.Property(p => p.EmployeeName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.PdfPath).HasMaxLength(512);
        builder.Property(p => p.CustomDeductionsJson).HasMaxLength(4000);

        foreach (var property in new[]
        {
            nameof(Payslip.Basic), nameof(Payslip.Housing), nameof(Payslip.Transport),
            nameof(Payslip.OtherAllowances), nameof(Payslip.GrossPay), nameof(Payslip.PayFactor),
            nameof(Payslip.CraMonthly), nameof(Payslip.Paye), nameof(Payslip.PensionEmployee),
            nameof(Payslip.PensionEmployer), nameof(Payslip.Nhf), nameof(Payslip.NsitfEmployer),
            nameof(Payslip.CustomDeductionsTotal), nameof(Payslip.NetPay),
        })
        {
            builder.Property(typeof(decimal), property).HasPrecision(18, 2);
        }

        builder.HasIndex(p => new { p.PayrollRunId, p.EmployeeId })
            .IsUnique()
            .HasDatabaseName("IX_Payslips_PayrollRunId_EmployeeId");

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(p => p.DomainEvents);
    }
}
