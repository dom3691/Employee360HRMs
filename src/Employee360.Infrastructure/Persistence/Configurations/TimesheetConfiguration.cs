using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Timesheet"/>.</summary>
public sealed class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.ToTable("Timesheets");

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.EmployeeId, t.WeekStart })
            .IsUnique()
            .HasDatabaseName("IX_Timesheets_EmployeeId_WeekStart");

        builder.Property(t => t.TotalHours)
            .HasPrecision(8, 2);

        builder.Property(t => t.Notes)
            .HasMaxLength(512);

        builder.Property(t => t.ReviewComments)
            .HasMaxLength(512);

        builder.HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(t => t.DomainEvents);
    }
}
