using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class PayrollStatutoryRateConfiguration : IEntityTypeConfiguration<PayrollStatutoryRate>
{
    public void Configure(EntityTypeBuilder<PayrollStatutoryRate> builder)
    {
        builder.ToTable("PayrollStatutoryRates");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(128).IsRequired();
        builder.Property(r => r.EmployeeRate).HasPrecision(8, 6);
        builder.Property(r => r.EmployerRate).HasPrecision(8, 6);
        builder.Property(r => r.FixedAnnualAmount).HasPrecision(18, 2);
        builder.Property(r => r.VariableRate).HasPrecision(8, 6);
        builder.HasIndex(r => new { r.TaxYear, r.Code })
            .IsUnique()
            .HasDatabaseName("IX_PayrollStatutoryRates_TaxYear_Code");
        builder.Ignore(r => r.DomainEvents);
    }
}
