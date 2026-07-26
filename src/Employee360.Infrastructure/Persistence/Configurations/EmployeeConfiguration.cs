using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Employee"/> (minimal Batch 4 shape).</summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("IX_Employees_Email");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Self-referencing reporting hierarchy; a manager cannot be hard-deleted
        // while reports point at them.
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ManagerId)
            .HasDatabaseName("IX_Employees_ManagerId");

        builder.Ignore(e => e.DomainEvents);
    }
}
