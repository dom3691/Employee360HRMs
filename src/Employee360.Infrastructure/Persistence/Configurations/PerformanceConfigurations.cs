using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class ReviewCycleConfiguration : IEntityTypeConfiguration<ReviewCycle>
{
    public void Configure(EntityTypeBuilder<ReviewCycle> builder)
    {
        builder.ToTable("ReviewCycles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(128).IsRequired();
        builder.Property(c => c.GoalWeightPercent).HasPrecision(5, 2);
        builder.Property(c => c.SelfWeightPercent).HasPrecision(5, 2);
        builder.Property(c => c.ManagerWeightPercent).HasPrecision(5, 2);
        builder.Property(c => c.PeerWeightPercent).HasPrecision(5, 2);
        builder.Ignore(c => c.DomainEvents);
    }
}

public sealed class EmployeeGoalConfiguration : IEntityTypeConfiguration<EmployeeGoal>
{
    public void Configure(EntityTypeBuilder<EmployeeGoal> builder)
    {
        builder.ToTable("EmployeeGoals");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Title).HasMaxLength(256).IsRequired();
        builder.Property(g => g.Weight).HasPrecision(5, 2);
        builder.Property(g => g.TargetValue).HasPrecision(18, 2);
        builder.Property(g => g.ActualValue).HasPrecision(18, 2);
        builder.HasOne(g => g.ReviewCycle).WithMany(c => c.Goals).HasForeignKey(g => g.ReviewCycleId);
        builder.HasOne(g => g.Employee).WithMany().HasForeignKey(g => g.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(g => new { g.ReviewCycleId, g.EmployeeId, g.Title })
            .HasDatabaseName("IX_EmployeeGoals_Cycle_Employee_Title");
        builder.Ignore(g => g.DomainEvents);
    }
}

public sealed class PerformanceReviewConfiguration : IEntityTypeConfiguration<PerformanceReview>
{
    public void Configure(EntityTypeBuilder<PerformanceReview> builder)
    {
        builder.ToTable("PerformanceReviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SelfRating).HasPrecision(3, 2);
        builder.Property(r => r.ManagerRating).HasPrecision(3, 2);
        builder.Property(r => r.FinalRating).HasPrecision(3, 2);
        builder.HasOne(r => r.ReviewCycle).WithMany(c => c.Reviews).HasForeignKey(r => r.ReviewCycleId);
        builder.HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.ManagerEmployee).WithMany().HasForeignKey(r => r.ManagerEmployeeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(r => new { r.ReviewCycleId, r.EmployeeId })
            .IsUnique()
            .HasDatabaseName("IX_PerformanceReviews_Cycle_Employee");
        builder.Ignore(r => r.DomainEvents);
    }
}

public sealed class PeerFeedbackConfiguration : IEntityTypeConfiguration<PeerFeedback>
{
    public void Configure(EntityTypeBuilder<PeerFeedback> builder)
    {
        builder.ToTable("PeerFeedbacks");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Rating).HasPrecision(3, 2);
        builder.Property(f => f.Comments).HasMaxLength(2000);
        builder.HasOne(f => f.PerformanceReview).WithMany(r => r.PeerFeedbacks).HasForeignKey(f => f.PerformanceReviewId);
        builder.HasIndex(f => new { f.PerformanceReviewId, f.ReviewerEmployeeId })
            .IsUnique()
            .HasDatabaseName("IX_PeerFeedbacks_Review_Reviewer");
        builder.Ignore(f => f.DomainEvents);
    }
}
