using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="EmployeeBankAccount"/> (FR-EMP-014).</summary>
public sealed class EmployeeBankAccountConfiguration : IEntityTypeConfiguration<EmployeeBankAccount>
{
    public void Configure(EntityTypeBuilder<EmployeeBankAccount> builder)
    {
        builder.ToTable("EmployeeBankAccounts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BankName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(b => b.BankCode)
            .HasMaxLength(10);

        // AES-256 ciphertext (base64 IV + payload) — never plain text (NFR-SEC-004).
        builder.Property(b => b.AccountNumberEncrypted)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(b => b.AccountName)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasOne(b => b.Employee)
            .WithOne(e => e.BankAccount)
            .HasForeignKey<EmployeeBankAccount>(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.EmployeeId)
            .IsUnique()
            .HasDatabaseName("IX_EmployeeBankAccounts_EmployeeId");

        builder.Ignore(b => b.DomainEvents);
    }
}
