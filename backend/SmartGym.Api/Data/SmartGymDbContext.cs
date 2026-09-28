using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data;

public class SmartGymDbContext : DbContext
{
    public SmartGymDbContext(DbContextOptions<SmartGymDbContext> options)
        : base(options)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Membership
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<ProgressRecord> ProgressRecords => Set<ProgressRecord>();

    // Classes
    public DbSet<ClassCategory> ClassCategories => Set<ClassCategory>();
    public DbSet<FitnessClass> FitnessClasses => Set<FitnessClass>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Inventory
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    // Facility
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FacilityIssue> FacilityIssues => Set<FacilityIssue>();
    public DbSet<IssueImage> IssueImages => Set<IssueImage>();
    public DbSet<RepairOrder> RepairOrders => Set<RepairOrder>();
    public DbSet<RepairOrderItem> RepairOrderItems => Set<RepairOrderItem>();
    public DbSet<Approval> Approvals => Set<Approval>();

    // AI
    public DbSet<AIWorkflow> AIWorkflows => Set<AIWorkflow>();
    public DbSet<AIWorkflowStep> AIWorkflowSteps => Set<AIWorkflowStep>();
    public DbSet<AIToolExecution> AIToolExecutions => Set<AIToolExecution>();
    public DbSet<AIValidationResult> AIValidationResults => Set<AIValidationResult>();

    // Supporting
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all fluent entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartGymDbContext).Assembly);

        // Global UTC DateTime value converter for PostgreSQL timestamptz compatibility
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null && createdAtProp.Metadata.ClrType == typeof(DateTime))
                {
                    if ((DateTime)createdAtProp.CurrentValue! == default)
                    {
                        createdAtProp.CurrentValue = now;
                    }
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null && (updatedAtProp.Metadata.ClrType == typeof(DateTime) || updatedAtProp.Metadata.ClrType == typeof(DateTime?)))
                {
                    updatedAtProp.CurrentValue = now;
                }
            }
        }
    }
}
