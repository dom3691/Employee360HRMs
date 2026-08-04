using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
{
    public void Configure(EntityTypeBuilder<SalaryStructure> builder)
    {
        builder.ToTable("SalaryStructures");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Basic).HasPrecision(18, 2);
        builder.Property(s => s.Housing).HasPrecision(18, 2);
        builder.Property(s => s.Transport).HasPrecision(18, 2);
        builder.Property(s => s.OtherAllowances).HasPrecision(18, 2);
        builder.HasIndex(s => s.Name).IsUnique().HasDatabaseName("IX_SalaryStructures_Name");
        builder.Ignore(s => s.GrossSalary);
        builder.Ignore(s => s.DomainEvents);
    }
}
