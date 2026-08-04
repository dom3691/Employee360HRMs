using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Employee"/> (FR-EMP-001..006).</summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(e => e.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("IX_Employees_EmployeeCode");

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.MiddleName)
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("IX_Employees_Email");

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(32);

        builder.Property(e => e.Nationality)
            .HasMaxLength(64);

        // AES-256 ciphertext (base64 IV + payload).
        builder.Property(e => e.NinEncrypted)
            .HasMaxLength(256);

        builder.Property(e => e.Address)
            .HasMaxLength(512);

        builder.Property(e => e.WorkLocation)
            .HasMaxLength(128);

        builder.Property(e => e.PensionPin)
            .HasMaxLength(32);

        builder.Property(e => e.PfaName)
            .HasMaxLength(128);

        builder.Property(e => e.Gender)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.MaritalStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.DepartmentId)
            .HasDatabaseName("IX_Employees_DepartmentId");

        builder.HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Self-referencing reporting hierarchy; a manager cannot be hard-deleted
        // while reports point at them.
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ManagerId)
            .HasDatabaseName("IX_Employees_ManagerId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Employees_Status");

        builder.Ignore(e => e.DomainEvents);
    }
}
