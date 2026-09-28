namespace SmartGym.Api.Entities;

public class Location
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Floor { get; set; } = "Ground Floor";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    public ICollection<FacilityIssue> FacilityIssues { get; set; } = new List<FacilityIssue>();
}

public class Equipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
    public DateTime? LastServicedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<FacilityIssue> FacilityIssues { get; set; } = new List<FacilityIssue>();
    public ICollection<RepairOrder> RepairOrders { get; set; } = new List<RepairOrder>();
}

public class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Pending;
    public string? AdminResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class FacilityIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportedByMemberId { get; set; }
    public Member ReportedBy { get; set; } = null!;

    public Guid? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SanitizedDescription { get; set; }
    public string? ResolutionNotes { get; set; }
    public string ModerationStatus { get; set; } = "Clean";
    public string? ModerationReason { get; set; }
    public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;
    public FacilityIssueStatus Status { get; set; } = FacilityIssueStatus.SUBMITTED;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<IssueImage> Images { get; set; } = new List<IssueImage>();
    public AIWorkflow? AIWorkflow { get; set; }
    public ICollection<RepairOrder> RepairOrders { get; set; } = new List<RepairOrder>();
}

public class IssueImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssueId { get; set; }
    public FacilityIssue FacilityIssue { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? OriginalFileName { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class RepairOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssueId { get; set; }
    public FacilityIssue FacilityIssue { get; set; } = null!;

    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public string OrderNumber { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public RepairOrderStatus Status { get; set; } = RepairOrderStatus.Draft;
    public string? TechnicianName { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<RepairOrderItem> Items { get; set; } = new List<RepairOrderItem>();
    public Approval? Approval { get; set; }
}

public class RepairOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public RepairOrder RepairOrder { get; set; } = null!;

    public string PartName { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class Approval
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public RepairOrder RepairOrder { get; set; } = null!;

    public Guid ApproverUserId { get; set; }
    public User Approver { get; set; } = null!;

    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Approved;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    public decimal ApprovalThreshold { get; set; } = 25000.0m;
    public decimal EstimatedCost { get; set; }
}
