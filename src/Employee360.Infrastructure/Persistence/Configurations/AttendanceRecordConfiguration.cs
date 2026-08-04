using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="AttendanceRecord"/>.</summary>
public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceRecords_EmployeeId_Date");

        builder.Property(a => a.SourceIp)
            .HasMaxLength(64);

        builder.Property(a => a.DeviceId)
            .HasMaxLength(64);

        builder.Property(a => a.CorrectionReason)
            .HasMaxLength(512);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Shift)
            .WithMany()
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(a => a.DomainEvents);
    }
}
