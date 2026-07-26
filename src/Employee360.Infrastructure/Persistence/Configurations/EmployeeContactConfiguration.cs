using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="EmployeeContact"/> (FR-EMP-013).</summary>
public sealed class EmployeeContactConfiguration : IEntityTypeConfiguration<EmployeeContact>
{
    public void Configure(EntityTypeBuilder<EmployeeContact> builder)
    {
        builder.ToTable("EmployeeContacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Relationship)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.Address)
            .HasMaxLength(512);

        builder.HasOne(c => c.Employee)
            .WithMany(e => e.Contacts)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.EmployeeId)
            .HasDatabaseName("IX_EmployeeContacts_EmployeeId");

        builder.Ignore(c => c.DomainEvents);
    }
}
