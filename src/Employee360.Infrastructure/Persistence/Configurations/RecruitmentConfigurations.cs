using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("JobPostings");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Title).HasMaxLength(256).IsRequired();
        builder.Property(j => j.Description).IsRequired();
        builder.HasOne(j => j.Department).WithMany().HasForeignKey(j => j.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(j => j.Status).HasDatabaseName("IX_JobPostings_Status");
        builder.Ignore(j => j.DomainEvents);
    }
}

public sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(32);
        builder.Property(c => c.ResumePath).HasMaxLength(512);
        builder.HasOne(c => c.JobPosting).WithMany(j => j.Candidates).HasForeignKey(c => c.JobPostingId);
        builder.HasOne(c => c.Employee).WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(c => new { c.JobPostingId, c.Email }).IsUnique().HasDatabaseName("IX_Candidates_JobPostingId_Email");
        builder.Ignore(c => c.DomainEvents);
    }
}

public sealed class OnboardingTaskConfiguration : IEntityTypeConfiguration<OnboardingTask>
{
    public void Configure(EntityTypeBuilder<OnboardingTask> builder)
    {
        builder.ToTable("OnboardingTasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TaskName).HasMaxLength(256).IsRequired();
        builder.HasOne(t => t.Employee).WithMany().HasForeignKey(t => t.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.AssignedToEmployee).WithMany().HasForeignKey(t => t.AssignedToEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.EmployeeId).HasDatabaseName("IX_OnboardingTasks_EmployeeId");
        builder.Ignore(t => t.DomainEvents);
    }
}
