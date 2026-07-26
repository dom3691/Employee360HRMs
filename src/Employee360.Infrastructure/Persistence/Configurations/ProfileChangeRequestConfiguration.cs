using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="ProfileChangeRequest"/> (FR-EMP-011).</summary>
public sealed class ProfileChangeRequestConfiguration : IEntityTypeConfiguration<ProfileChangeRequest>
{
    public void Configure(EntityTypeBuilder<ProfileChangeRequest> builder)
    {
        builder.ToTable("ProfileChangeRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ChangesJson)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(r => r.ReviewComment)
            .HasMaxLength(512);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.EmployeeId, r.Status })
            .HasDatabaseName("IX_ProfileChangeRequests_EmployeeId_Status");

        builder.Ignore(r => r.DomainEvents);
    }
}
