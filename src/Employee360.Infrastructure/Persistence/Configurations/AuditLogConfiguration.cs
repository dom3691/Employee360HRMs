using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for the <see cref="AuditLog"/> audit trail table.</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(16)
            .IsRequired();

        // JSON snapshots; size is unbounded by design (nvarchar(max)).
        builder.Property(a => a.OldValues);
        builder.Property(a => a.NewValues);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityName_EntityId");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        // Domain events are an in-memory concern only.
        builder.Ignore(a => a.DomainEvents);
    }
}
