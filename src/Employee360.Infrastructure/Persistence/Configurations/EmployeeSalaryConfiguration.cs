using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class EmployeeSalaryConfiguration : IEntityTypeConfiguration<EmployeeSalary>
{
    public void Configure(EntityTypeBuilder<EmployeeSalary> builder)
    {
        builder.ToTable("EmployeeSalaries");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Basic).HasPrecision(18, 2);
        builder.Property(s => s.Housing).HasPrecision(18, 2);
        builder.Property(s => s.Transport).HasPrecision(18, 2);
        builder.Property(s => s.OtherAllowances).HasPrecision(18, 2);
        builder.HasIndex(s => new { s.EmployeeId, s.EffectiveDate })
            .HasDatabaseName("IX_EmployeeSalaries_EmployeeId_EffectiveDate");
        builder.HasOne(s => s.Employee).WithMany(e => e.Salaries).HasForeignKey(s => s.EmployeeId);
        builder.HasOne(s => s.SalaryStructure).WithMany(st => st.EmployeeSalaries).HasForeignKey(s => s.SalaryStructureId);
        builder.Ignore(s => s.GrossSalary);
        builder.Ignore(s => s.DomainEvents);
    }
}
