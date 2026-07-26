using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="LeaveType"/> (FR-LV-001).</summary>
public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique().HasDatabaseName("IX_LeaveTypes_Name");

        builder.Property(t => t.Code).HasMaxLength(16).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("IX_LeaveTypes_Code");

        builder.Property(t => t.Color).HasMaxLength(16);

        builder.HasOne(t => t.Policy)
            .WithOne(p => p.LeaveType)
            .HasForeignKey<LeavePolicy>(p => p.LeaveTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.DomainEvents);
    }
}

/// <summary>EF Core mapping for <see cref="LeavePolicy"/> (FR-LV-002).</summary>
public sealed class LeavePolicyConfiguration : IEntityTypeConfiguration<LeavePolicy>
{
    public void Configure(EntityTypeBuilder<LeavePolicy> builder)
    {
        builder.ToTable("LeavePolicies");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.LeaveTypeId)
            .IsUnique()
            .HasDatabaseName("IX_LeavePolicies_LeaveTypeId");

        builder.Property(p => p.AnnualEntitlement).HasPrecision(5, 1);
        builder.Property(p => p.CarryForwardMax).HasPrecision(5, 1);

        builder.Property(p => p.AccrualFrequency)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Ignore(p => p.DomainEvents);
    }
}

/// <summary>EF Core mapping for <see cref="LeaveBalance"/> (FR-LV-004).</summary>
public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances");
        builder.HasKey(b => b.Id);

        // One balance row per employee + type + year.
        builder.HasIndex(b => new { b.EmployeeId, b.LeaveTypeId, b.Year })
            .IsUnique()
            .HasDatabaseName("IX_LeaveBalances_Employee_Type_Year");

        builder.Property(b => b.Entitled).HasPrecision(5, 1);
        builder.Property(b => b.Used).HasPrecision(5, 1);
        builder.Property(b => b.Pending).HasPrecision(5, 1);
        builder.Property(b => b.CarriedForward).HasPrecision(5, 1);

        builder.HasOne(b => b.Employee)
            .WithMany()
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.LeaveType)
            .WithMany()
            .HasForeignKey(b => b.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(b => b.Available);
        builder.Ignore(b => b.DomainEvents);
    }
}

/// <summary>EF Core mapping for <see cref="LeaveRequest"/> (FR-LV-005..009).</summary>
public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Days).HasPrecision(5, 1);
        builder.Property(r => r.Reason).HasMaxLength(1024).IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.LeaveType)
            .WithMany()
            .HasForeignKey(r => r.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Approver)
            .WithMany()
            .HasForeignKey(r => r.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.EmployeeId, r.Status })
            .HasDatabaseName("IX_LeaveRequests_EmployeeId_Status");

        builder.HasIndex(r => new { r.ApproverId, r.Status })
            .HasDatabaseName("IX_LeaveRequests_ApproverId_Status");

        builder.Ignore(r => r.DomainEvents);
    }
}

/// <summary>EF Core mapping for <see cref="LeaveApproval"/> (FR-LV-008).</summary>
public sealed class LeaveApprovalConfiguration : IEntityTypeConfiguration<LeaveApproval>
{
    public void Configure(EntityTypeBuilder<LeaveApproval> builder)
    {
        builder.ToTable("LeaveApprovals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Comments).HasMaxLength(1024);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(a => a.LeaveRequest)
            .WithMany(r => r.Approvals)
            .HasForeignKey(a => a.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(a => a.DomainEvents);
    }
}

/// <summary>EF Core mapping for <see cref="PublicHoliday"/> (FR-ADM-002).</summary>
public sealed class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("PublicHolidays");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(h => h.Date)
            .IsUnique()
            .HasDatabaseName("IX_PublicHolidays_Date");

        builder.HasIndex(h => h.Year)
            .HasDatabaseName("IX_PublicHolidays_Year");

        builder.Ignore(h => h.DomainEvents);
    }
}
