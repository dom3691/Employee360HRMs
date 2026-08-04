using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Shift"/>.</summary>
public sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_Shifts_Name");

        builder.Ignore(s => s.DomainEvents);
    }
}
