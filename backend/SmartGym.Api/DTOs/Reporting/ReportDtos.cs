namespace SmartGym.Api.DTOs.Reporting;

public class MembershipReportDto
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int ExpiredMemberships { get; set; }
    public int ExpiringSoonCount { get; set; } // Expiring within 14 days
    public decimal TotalRevenue { get; set; }
    public List<MembershipPlanDistributionDto> PlanDistribution { get; set; } = new();
    public List<ExpiringMembershipDto> ExpiringMemberships { get; set; } = new();
}

public class MembershipPlanDistributionDto
{
    public string PlanName { get; set; } = string.Empty;
    public int SubscribersCount { get; set; }
    public decimal SharePercentage { get; set; }
    public decimal RevenueGenerated { get; set; }
}

public class ExpiringMembershipDto
{
    public Guid MembershipId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
}

public class ClassesReportDto
{
    public int TotalClasses { get; set; }
    public int TotalSchedules { get; set; }
    public int TotalBookings { get; set; }
    public int TotalAttended { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalCancelled { get; set; }
    public double AttendanceRatePercentage { get; set; }
    public double OverallCapacityUtilizationPercentage { get; set; }
    public List<ClassPopularityDto> PopularClasses { get; set; } = new();
}

public class ClassPopularityDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public double AverageUtilization { get; set; }
}

public class InventoryReportDto
{
    public int TotalProducts { get; set; }
    public int TotalInventoryItems { get; set; }
    public int LowStockItemsCount { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalStockMovements { get; set; }
    public List<StockMovementSummaryDto> MovementsByType { get; set; } = new();
    public List<SupplierActivityDto> SupplierActivity { get; set; } = new();
    public List<LowStockItemDto> CriticalStockItems { get; set; } = new();
}

public class StockMovementSummaryDto
{
    public string MovementType { get; set; } = string.Empty;
    public int TotalMovements { get; set; }
    public int TotalQuantityChanged { get; set; }
}

public class SupplierActivityDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int ProductsSuppliedCount { get; set; }
    public int PurchaseOrdersCount { get; set; }
}

public class LowStockItemDto
{
    public Guid ItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderThreshold { get; set; }
    public string LocationName { get; set; } = string.Empty;
}

public class FacilityReportDto
{
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int InProgressIssues { get; set; }
    public int ResolvedIssues { get; set; }
    public double AverageResolutionHours { get; set; }
    public decimal TotalRepairCosts { get; set; }
    public Dictionary<string, int> IssuesByPriority { get; set; } = new();
    public List<EquipmentIssueSummaryDto> IssuesByEquipment { get; set; } = new();
}

public class EquipmentIssueSummaryDto
{
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int IssueCount { get; set; }
    public decimal TotalRepairCost { get; set; }
}

public class AiReportDto
{
    public int TotalWorkflows { get; set; }
    public int PendingApprovalsCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int RevisionsCount { get; set; }
    public int SafeFailuresCount { get; set; }
    public double AverageWorkflowDurationSeconds { get; set; }
}

public class ExecutiveDashboardDto
{
    public MembershipReportDto Membership { get; set; } = new();
    public ClassesReportDto Classes { get; set; } = new();
    public InventoryReportDto Inventory { get; set; } = new();
    public FacilityReportDto Facility { get; set; } = new();
    public AiReportDto Ai { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
