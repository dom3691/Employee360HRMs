using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="User"/> authentication accounts.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        // Optional 1:1 with Employee; a user account may exist without an employee
        // record (e.g. dedicated System Admin accounts).
        builder.HasOne(u => u.Employee)
            .WithOne()
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => u.EmployeeId)
            .IsUnique()
            .HasFilter("\"EmployeeId\" IS NOT NULL")
            .HasDatabaseName("IX_Users_EmployeeId");

        builder.Ignore(u => u.DomainEvents);
    }
}
