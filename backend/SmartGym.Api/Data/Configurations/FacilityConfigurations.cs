using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(l => l.Name);

        builder.Property(l => l.Floor)
            .HasMaxLength(50);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.HasMany(l => l.Equipments)
            .WithOne(e => e.Location)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.FacilityIssues)
            .WithOne(fi => fi.Location)
            .HasForeignKey(fi => fi.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SerialNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.SerialNumber)
            .IsUnique();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Model)
            .HasMaxLength(100);

        builder.Property(e => e.Manufacturer)
            .HasMaxLength(100);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.PurchaseDate);
        builder.HasIndex(e => e.LocationId);

        builder.HasOne(e => e.Location)
            .WithMany(l => l.Equipments)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.FacilityIssues)
            .WithOne(fi => fi.Equipment)
            .HasForeignKey(fi => fi.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.RepairOrders)
            .WithOne(ro => ro.Equipment)
            .HasForeignKey(ro => ro.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedbacks");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_feedbacks_rating", "\"Rating\" BETWEEN 1 AND 5");
        });

        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.Rating);
        builder.HasIndex(f => f.CreatedAt);

        builder.HasOne(f => f.Member)
            .WithMany()
            .HasForeignKey(f => f.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FacilityIssueConfiguration : IEntityTypeConfiguration<FacilityIssue>
{
    public void Configure(EntityTypeBuilder<FacilityIssue> builder)
    {
        builder.ToTable("facility_issues");
        builder.HasKey(fi => fi.Id);

        builder.Property(fi => fi.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fi => fi.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(fi => fi.SanitizedDescription)
            .HasMaxLength(2000);

        builder.Property(fi => fi.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(fi => fi.ModerationStatus)
            .HasMaxLength(50);

        builder.Property(fi => fi.ModerationReason)
            .HasMaxLength(500);

        builder.HasIndex(fi => fi.Status);
        builder.HasIndex(fi => fi.Severity);
        builder.HasIndex(fi => fi.ReportedAt);
        builder.HasIndex(fi => fi.EquipmentId);
        builder.HasIndex(fi => fi.LocationId);

        builder.HasOne(fi => fi.ReportedBy)
            .WithMany()
            .HasForeignKey(fi => fi.ReportedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fi => fi.Equipment)
            .WithMany(e => e.FacilityIssues)
            .HasForeignKey(fi => fi.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fi => fi.Location)
            .WithMany(l => l.FacilityIssues)
            .HasForeignKey(fi => fi.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(fi => fi.Images)
            .WithOne(img => img.FacilityIssue)
            .HasForeignKey(img => img.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fi => fi.AIWorkflow)
            .WithOne(ai => ai.FacilityIssue)
            .HasForeignKey<AIWorkflow>(ai => ai.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(fi => fi.RepairOrders)
            .WithOne(ro => ro.FacilityIssue)
            .HasForeignKey(ro => ro.IssueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class IssueImageConfiguration : IEntityTypeConfiguration<IssueImage>
{
    public void Configure(EntityTypeBuilder<IssueImage> builder)
    {
        builder.ToTable("issue_images");
        builder.HasKey(img => img.Id);

        builder.Property(img => img.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(img => img.ThumbnailUrl)
            .HasMaxLength(1000);

        builder.Property(img => img.ContentType)
            .HasMaxLength(100);

        builder.Property(img => img.OriginalFileName)
            .HasMaxLength(255);

        builder.HasOne(img => img.FacilityIssue)
            .WithMany(fi => fi.Images)
            .HasForeignKey(img => img.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RepairOrderConfiguration : IEntityTypeConfiguration<RepairOrder>
{
    public void Configure(EntityTypeBuilder<RepairOrder> builder)
    {
        builder.ToTable("repair_orders");
        builder.HasKey(ro => ro.Id);

        builder.Property(ro => ro.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ro => ro.OrderNumber)
            .IsUnique();

        builder.Property(ro => ro.EstimatedCost)
            .HasPrecision(12, 2);

        builder.Property(ro => ro.ActualCost)
            .HasPrecision(12, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_repair_orders_estimated_cost", "\"EstimatedCost\" >= 0");
            t.HasCheckConstraint("CK_repair_orders_actual_cost", "\"ActualCost\" IS NULL OR \"ActualCost\" >= 0");
        });

        builder.HasIndex(ro => ro.Status);
        builder.HasIndex(ro => ro.CreatedAt);
        builder.HasIndex(ro => ro.EquipmentId);

        builder.HasOne(ro => ro.FacilityIssue)
            .WithMany(fi => fi.RepairOrders)
            .HasForeignKey(ro => ro.IssueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ro => ro.Equipment)
            .WithMany(e => e.RepairOrders)
            .HasForeignKey(ro => ro.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ro => ro.Items)
            .WithOne(roi => roi.RepairOrder)
            .HasForeignKey(roi => roi.RepairOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ro => ro.Approval)
            .WithOne(a => a.RepairOrder)
            .HasForeignKey<Approval>(a => a.RepairOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RepairOrderItemConfiguration : IEntityTypeConfiguration<RepairOrderItem>
{
    public void Configure(EntityTypeBuilder<RepairOrderItem> builder)
    {
        builder.ToTable("repair_order_items");
        builder.HasKey(roi => roi.Id);

        builder.Property(roi => roi.PartName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(roi => roi.UnitCost)
            .HasPrecision(10, 2);

        builder.Property(roi => roi.TotalCost)
            .HasPrecision(12, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_repair_order_items_quantity", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_repair_order_items_unit_cost", "\"UnitCost\" >= 0");
            t.HasCheckConstraint("CK_repair_order_items_total_cost", "\"TotalCost\" >= 0");
        });

        builder.HasOne(roi => roi.RepairOrder)
            .WithMany(ro => ro.Items)
            .HasForeignKey(roi => roi.RepairOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("approvals");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.RepairOrderId)
            .IsUnique();

        builder.Property(a => a.ApprovalThreshold)
            .HasPrecision(12, 2);

        builder.Property(a => a.EstimatedCost)
            .HasPrecision(12, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_approvals_threshold", "\"ApprovalThreshold\" >= 0");
            t.HasCheckConstraint("CK_approvals_estimated_cost", "\"EstimatedCost\" >= 0");
        });

        builder.HasIndex(a => a.Decision);
        builder.HasIndex(a => a.DecidedAt);

        builder.HasOne(a => a.RepairOrder)
            .WithOne(ro => ro.Approval)
            .HasForeignKey<Approval>(a => a.RepairOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Approver)
            .WithMany(u => u.ApprovalsGiven)
            .HasForeignKey(a => a.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
