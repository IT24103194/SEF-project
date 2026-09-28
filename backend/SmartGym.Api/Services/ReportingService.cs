using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Reporting;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public class ReportingService : IReportingService
{
    private readonly SmartGymDbContext _context;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(SmartGymDbContext context, ILogger<ReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MembershipReportDto> GetMembershipReportAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var soonThreshold = now.AddDays(14);

        var totalMembers = await _context.Members.CountAsync(cancellationToken);

        var memberships = await _context.Memberships
            .AsNoTracking()
            .Include(m => m.Plan)
            .Include(m => m.Member).ThenInclude(mem => mem.User)
            .ToListAsync(cancellationToken);

        var activeMemberships = memberships
            .Where(m => m.Status == MembershipStatus.Active && m.EndDate >= now)
            .ToList();

        var activeMemberIds = activeMemberships.Select(m => m.MemberId).Distinct().Count();

        var expiredMemberships = memberships
            .Count(m => m.Status == MembershipStatus.Expired || m.EndDate < now);

        var expiringSoon = activeMemberships
            .Where(m => m.EndDate >= now && m.EndDate <= soonThreshold)
            .OrderBy(m => m.EndDate)
            .ToList();

        var totalRevenue = memberships.Sum(m => m.PricePaid);

        // Plan distribution
        var totalActive = activeMemberships.Count;
        var planDistribution = memberships
            .GroupBy(m => m.Plan.Name)
            .Select(g => new MembershipPlanDistributionDto
            {
                PlanName = g.Key,
                SubscribersCount = g.Count(m => m.Status == MembershipStatus.Active && m.EndDate >= now),
                SharePercentage = totalActive > 0
                    ? Math.Round((decimal)g.Count(m => m.Status == MembershipStatus.Active && m.EndDate >= now) / totalActive * 100, 1)
                    : 0,
                RevenueGenerated = g.Sum(m => m.PricePaid)
            })
            .OrderByDescending(p => p.SubscribersCount)
            .ToList();

        var expiringList = expiringSoon
            .Take(10)
            .Select(m => new ExpiringMembershipDto
            {
                MembershipId = m.Id,
                MemberId = m.MemberId,
                MemberName = $"{m.Member.User.FirstName} {m.Member.User.LastName}",
                MemberEmail = m.Member.User.Email,
                PlanName = m.Plan.Name,
                EndDate = m.EndDate,
                DaysRemaining = Math.Max(0, (m.EndDate - now).Days)
            })
            .ToList();

        return new MembershipReportDto
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMemberIds,
            ExpiredMemberships = expiredMemberships,
            ExpiringSoonCount = expiringSoon.Count,
            TotalRevenue = totalRevenue,
            PlanDistribution = planDistribution,
            ExpiringMemberships = expiringList
        };
    }

    public async Task<ClassesReportDto> GetClassesReportAsync(CancellationToken cancellationToken = default)
    {
        var totalClasses = await _context.FitnessClasses.CountAsync(cancellationToken);
        var totalSchedules = await _context.ClassSchedules.CountAsync(cancellationToken);
        var totalBookings = await _context.Bookings.CountAsync(cancellationToken);

        var attendances = await _context.Attendances.AsNoTracking().ToListAsync(cancellationToken);
        var totalAttended = attendances.Count(a => a.Status == AttendanceStatus.Attended);
        var totalAbsent = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        var totalCancelled = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Cancelled, cancellationToken);

        var totalRecordedAttendance = totalAttended + totalAbsent;
        var attendanceRate = totalRecordedAttendance > 0
            ? Math.Round((double)totalAttended / totalRecordedAttendance * 100, 1)
            : 0.0;

        // Overall Capacity Utilization
        var schedules = await _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.FitnessClass).ThenInclude(c => c.Category)
            .Include(s => s.Bookings)
            .ToListAsync(cancellationToken);

        var totalCapacity = schedules.Sum(s => s.Capacity);
        var totalBookedSlots = schedules.Sum(s => s.Bookings.Count(b => b.Status == BookingStatus.Confirmed));
        var overallUtilization = totalCapacity > 0
            ? Math.Round((double)totalBookedSlots / totalCapacity * 100, 1)
            : 0.0;

        // Popular classes
        var popularClasses = schedules
            .GroupBy(s => new { s.ClassId, s.FitnessClass.Name, CategoryName = s.FitnessClass.Category.Name })
            .Select(g =>
            {
                var schedCapacity = g.Sum(s => s.Capacity);
                var schedBooked = g.Sum(s => s.Bookings.Count(b => b.Status == BookingStatus.Confirmed));
                var util = schedCapacity > 0 ? Math.Round((double)schedBooked / schedCapacity * 100, 1) : 0;
                return new ClassPopularityDto
                {
                    ClassId = g.Key.ClassId,
                    ClassName = g.Key.Name,
                    CategoryName = g.Key.CategoryName,
                    TotalBookings = schedBooked,
                    AverageUtilization = util
                };
            })
            .OrderByDescending(c => c.TotalBookings)
            .Take(10)
            .ToList();

        return new ClassesReportDto
        {
            TotalClasses = totalClasses,
            TotalSchedules = totalSchedules,
            TotalBookings = totalBookings,
            TotalAttended = totalAttended,
            TotalAbsent = totalAbsent,
            TotalCancelled = totalCancelled,
            AttendanceRatePercentage = attendanceRate,
            OverallCapacityUtilizationPercentage = overallUtilization,
            PopularClasses = popularClasses
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default)
    {
        var totalProducts = await _context.Products.CountAsync(cancellationToken);
        var totalItems = await _context.InventoryItems.CountAsync(cancellationToken);
        var totalSuppliers = await _context.Suppliers.CountAsync(cancellationToken);
        var totalStockMovements = await _context.StockMovements.CountAsync(cancellationToken);

        var inventoryItems = await _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Product)
            .ToListAsync(cancellationToken);

        var lowStockItems = inventoryItems
            .Where(i => i.QuantityInStock <= i.ReorderThreshold)
            .OrderBy(i => i.QuantityInStock)
            .ToList();

        var stockMovements = await _context.StockMovements
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var movementsByType = stockMovements
            .GroupBy(m => m.MovementType.ToString())
            .Select(g => new StockMovementSummaryDto
            {
                MovementType = g.Key,
                TotalMovements = g.Count(),
                TotalQuantityChanged = g.Sum(m => Math.Abs(m.QuantityChange))
            })
            .ToList();

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.Products)
            .Include(s => s.PurchaseOrders)
            .Select(s => new SupplierActivityDto
            {
                SupplierId = s.Id,
                SupplierName = s.Name,
                ProductsSuppliedCount = s.Products.Count(),
                PurchaseOrdersCount = s.PurchaseOrders.Count()
            })
            .OrderByDescending(s => s.ProductsSuppliedCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        var criticalStock = lowStockItems
            .Take(10)
            .Select(i => new LowStockItemDto
            {
                ItemId = i.Id,
                ProductName = i.Product.Name,
                Sku = i.Product.SKU,
                CurrentStock = i.QuantityInStock,
                ReorderThreshold = i.ReorderThreshold,
                LocationName = i.LocationBin ?? "General Bin"
            })
            .ToList();

        return new InventoryReportDto
        {
            TotalProducts = totalProducts,
            TotalInventoryItems = totalItems,
            LowStockItemsCount = lowStockItems.Count,
            TotalSuppliers = totalSuppliers,
            TotalStockMovements = totalStockMovements,
            MovementsByType = movementsByType,
            SupplierActivity = suppliers,
            CriticalStockItems = criticalStock
        };
    }

    public async Task<FacilityReportDto> GetFacilityReportAsync(CancellationToken cancellationToken = default)
    {
        var issues = await _context.FacilityIssues
            .AsNoTracking()
            .Include(i => i.Equipment)
            .Include(i => i.Location)
            .ToListAsync(cancellationToken);

        var totalIssues = issues.Count;

        var openStatuses = new HashSet<FacilityIssueStatus>
        {
            FacilityIssueStatus.SUBMITTED,
            FacilityIssueStatus.AI_ANALYZING,
            FacilityIssueStatus.PENDING_APPROVAL,
            FacilityIssueStatus.APPROVED,
            FacilityIssueStatus.VENDOR_CONTACTED,
            FacilityIssueStatus.REPAIR_SCHEDULED
        };

        var openIssues = issues.Count(i => openStatuses.Contains(i.Status));
        var inProgressIssues = issues.Count(i => i.Status == FacilityIssueStatus.IN_PROGRESS);
        var resolvedIssues = issues.Count(i => i.Status == FacilityIssueStatus.RESOLVED);

        // Average Resolution Time (in hours)
        var resolvedList = issues.Where(i => i.Status == FacilityIssueStatus.RESOLVED && i.ResolvedAt.HasValue).ToList();
        var avgHours = resolvedList.Any()
            ? Math.Round(resolvedList.Average(i => (i.ResolvedAt!.Value - i.CreatedAt).TotalHours), 1)
            : 0.0;

        // Repair costs
        var repairOrders = await _context.RepairOrders.AsNoTracking().ToListAsync(cancellationToken);
        var totalCosts = repairOrders.Sum(r => r.ActualCost ?? r.EstimatedCost);

        // Issues by Priority
        var issuesByPriority = issues
            .GroupBy(i => i.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Issues by Equipment
        var issuesByEquipment = issues
            .Where(i => i.EquipmentId.HasValue && i.Equipment != null)
            .GroupBy(i => new { i.EquipmentId, EquipmentName = i.Equipment!.Name, LocationName = i.Location.Name })
            .Select(g => new EquipmentIssueSummaryDto
            {
                EquipmentId = g.Key.EquipmentId!.Value,
                EquipmentName = g.Key.EquipmentName,
                LocationName = g.Key.LocationName,
                IssueCount = g.Count(),
                TotalRepairCost = 0m
            })
            .OrderByDescending(e => e.IssueCount)
            .Take(10)
            .ToList();

        return new FacilityReportDto
        {
            TotalIssues = totalIssues,
            OpenIssues = openIssues,
            InProgressIssues = inProgressIssues,
            ResolvedIssues = resolvedIssues,
            AverageResolutionHours = avgHours,
            TotalRepairCosts = totalCosts,
            IssuesByPriority = issuesByPriority,
            IssuesByEquipment = issuesByEquipment
        };
    }

    public async Task<AiReportDto> GetAiReportAsync(CancellationToken cancellationToken = default)
    {
        var workflows = await _context.AIWorkflows
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var approvals = await _context.Approvals
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalWorkflows = workflows.Count;
        var pendingApprovals = workflows.Count(w => w.Status == AIWorkflowStatus.AwaitingApproval);

        var approved = approvals.Count(a => a.Decision == ApprovalDecision.Approved);
        var rejected = approvals.Count(a => a.Decision == ApprovalDecision.Rejected);
        var revisions = approvals.Count(a => a.Decision == ApprovalDecision.Revised);
        var failures = workflows.Count(w => w.Status == AIWorkflowStatus.Failed);

        var completedWorkflows = workflows.Where(w => w.CompletedAt.HasValue).ToList();
        var avgDuration = completedWorkflows.Any()
            ? Math.Round(completedWorkflows.Average(w => (w.CompletedAt!.Value - w.CreatedAt).TotalSeconds), 1)
            : 0.0;

        return new AiReportDto
        {
            TotalWorkflows = totalWorkflows,
            PendingApprovalsCount = pendingApprovals,
            ApprovedCount = approved,
            RejectedCount = rejected,
            RevisionsCount = revisions,
            SafeFailuresCount = failures,
            AverageWorkflowDurationSeconds = avgDuration
        };
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipReportAsync(cancellationToken);
        var classes = await GetClassesReportAsync(cancellationToken);
        var inventory = await GetInventoryReportAsync(cancellationToken);
        var facility = await GetFacilityReportAsync(cancellationToken);
        var ai = await GetAiReportAsync(cancellationToken);

        return new ExecutiveDashboardDto
        {
            Membership = membership,
            Classes = classes,
            Inventory = inventory,
            Facility = facility,
            Ai = ai,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
