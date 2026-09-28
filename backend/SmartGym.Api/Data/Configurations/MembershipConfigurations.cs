using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.UserId)
            .IsUnique();

        builder.Property(m => m.EmergencyContactName)
            .HasMaxLength(150);

        builder.Property(m => m.EmergencyContactPhone)
            .HasMaxLength(50);

        builder.Property(m => m.MedicalConditions)
            .HasMaxLength(1000);

        builder.Property(m => m.Address)
            .HasMaxLength(250);

        builder.Property(m => m.Gender)
            .HasMaxLength(20);

        builder.HasIndex(m => m.JoinDate);

        builder.HasOne(m => m.User)
            .WithOne(u => u.MemberProfile)
            .HasForeignKey<Member>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Memberships)
            .WithOne(ms => ms.Member)
            .HasForeignKey(ms => ms.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Goals)
            .WithOne(g => g.Member)
            .HasForeignKey(g => g.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Bookings)
            .WithOne(b => b.Member)
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Attendances)
            .WithOne(a => a.Member)
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.ReportedIssues)
            .WithOne(fi => fi.ReportedBy)
            .HasForeignKey(fi => fi.ReportedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Feedbacks)
            .WithOne(f => f.Member)
            .HasForeignKey(f => f.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MembershipPlanConfiguration : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> builder)
    {
        builder.ToTable("membership_plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Price)
            .HasPrecision(10, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_membership_plans_price", "\"Price\" >= 0");
            t.HasCheckConstraint("CK_membership_plans_duration", "\"DurationDays\" > 0");
        });

        builder.HasIndex(p => p.IsActive);
    }
}

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.PricePaid)
            .HasPrecision(10, 2);

        builder.HasIndex(m => new { m.MemberId, m.Status });
        builder.HasIndex(m => m.EndDate);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_memberships_price_paid", "\"PricePaid\" >= 0");
        });

        builder.HasOne(m => m.Member)
            .WithMany(mem => mem.Memberships)
            .HasForeignKey(m => m.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Plan)
            .WithMany(p => p.Memberships)
            .HasForeignKey(m => m.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Unit)
            .HasMaxLength(20);

        builder.Property(g => g.TargetValue)
            .HasPrecision(10, 2);

        builder.Property(g => g.CurrentValue)
            .HasPrecision(10, 2);

        builder.HasIndex(g => new { g.MemberId, g.Status });
        builder.HasIndex(g => g.TargetDate);

        builder.HasOne(g => g.Member)
            .WithMany(m => m.Goals)
            .HasForeignKey(g => g.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.ProgressRecords)
            .WithOne(pr => pr.Goal)
            .HasForeignKey(pr => pr.GoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProgressRecordConfiguration : IEntityTypeConfiguration<ProgressRecord>
{
    public void Configure(EntityTypeBuilder<ProgressRecord> builder)
    {
        builder.ToTable("progress_records");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Value)
            .HasPrecision(10, 2);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.HasIndex(p => new { p.GoalId, p.RecordedDate });

        builder.HasOne(p => p.Goal)
            .WithMany(g => g.ProgressRecords)
            .HasForeignKey(p => p.GoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
