using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Position"/> (FR-EMP-010).</summary>
public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("IX_Positions_Code");

        builder.HasOne(p => p.Department)
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Grade)
            .WithMany(g => g.Positions)
            .HasForeignKey(p => p.GradeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(p => p.DomainEvents);
    }
}
