using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PeriodLabel).HasMaxLength(64).IsRequired();
        builder.Property(r => r.LastError).HasMaxLength(2000);
        builder.Property(r => r.CalculationJobId).HasMaxLength(128);

        builder.HasIndex(r => new { r.PeriodYear, r.PeriodMonth })
            .IsUnique()
            .HasDatabaseName("IX_PayrollRuns_PeriodYear_PeriodMonth");

        builder.HasMany(r => r.Payslips)
            .WithOne(p => p.PayrollRun)
            .HasForeignKey(p => p.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.DomainEvents);
    }
}
