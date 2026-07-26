using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Department"/> (FR-EMP-009).</summary>
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(d => d.Code)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasDatabaseName("IX_Departments_Code");

        builder.HasOne(d => d.ParentDepartment)
            .WithMany()
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.HeadEmployee)
            .WithMany()
            .HasForeignKey(d => d.HeadEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(d => d.DomainEvents);
    }
}
