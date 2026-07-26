using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="EmployeeDocument"/> (FR-EMP-007).</summary>
public sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("EmployeeDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Category)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.FileName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(d => d.StoragePath)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasOne(d => d.Employee)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.EmployeeId)
            .HasDatabaseName("IX_EmployeeDocuments_EmployeeId");

        builder.HasIndex(d => d.ExpiryDate)
            .HasDatabaseName("IX_EmployeeDocuments_ExpiryDate");

        builder.Ignore(d => d.DomainEvents);
    }
}
