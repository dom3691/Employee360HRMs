using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class PayrollDeductionConfiguration : IEntityTypeConfiguration<PayrollDeduction>
{
    public void Configure(EntityTypeBuilder<PayrollDeduction> builder)
    {
        builder.ToTable("PayrollDeductions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(128).IsRequired();
        builder.Property(d => d.FixedAmount).HasPrecision(18, 2);
        builder.Property(d => d.PercentOfGross).HasPrecision(8, 6);
        builder.HasOne(d => d.Employee).WithMany(e => e.PayrollDeductions).HasForeignKey(d => d.EmployeeId);
        builder.Ignore(d => d.DomainEvents);
    }
}
