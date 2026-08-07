using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Grade"/>.</summary>
public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(g => g.Name)
            .IsUnique()
            .HasDatabaseName("IX_Grades_Name");

        builder.HasIndex(g => g.Level)
            .IsUnique()
            .HasDatabaseName("IX_Grades_Level");

        // Currency convention: decimal(18,2), displayed as ₦.
        builder.Property(g => g.MinSalary)
            .HasPrecision(18, 2);

        builder.Property(g => g.MaxSalary)
            .HasPrecision(18, 2);

        builder.Property(g => g.Code)
            .HasMaxLength(16)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(g => g.LevelRank)
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(g => g.Description)
            .HasMaxLength(512);

        builder.HasIndex(g => g.Code)
            .IsUnique()
            .HasDatabaseName("IX_Grades_Code");

        builder.Ignore(g => g.DomainEvents);
    }
}
