using System.ComponentModel.DataAnnotations;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;

namespace SmartGym.Api.DTOs.Facility;

#region Location DTOs
public class LocationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EquipmentCount { get; set; }
    public int ActiveIssuesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLocationRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Floor { get; set; } = "Ground Floor";

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateLocationRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Floor { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
#endregion

#region Equipment DTOs
public class EquipmentDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationFloor { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public EquipmentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? LastServicedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ActiveIssuesCount { get; set; }
}

public class CreateEquipmentRequest
{
    [Required]
    public Guid LocationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? WarrantyExpiryDate { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
}

public class UpdateEquipmentRequest
{
    [Required]
    public Guid LocationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public EquipmentStatus Status { get; set; }
    public DateTime? LastServicedDate { get; set; }
}

public class EquipmentHistoryDto
{
    public EquipmentDto Equipment { get; set; } = null!;
    public IReadOnlyList<FacilityIssueSummaryDto> Issues { get; set; } = Array.Empty<FacilityIssueSummaryDto>();
    public IReadOnlyList<RepairOrderDto> RepairOrders { get; set; } = Array.Empty<RepairOrderDto>();
    public int TotalIssuesCount { get; set; }
    public int TotalRepairsCount { get; set; }
    public decimal TotalRepairCost { get; set; }
}
#endregion

#region Facility Issue DTOs
public enum IssueUrgencyLevel
{
    Normal = 0,
    Elevated = 1,
    CriticalEmergency = 2
}

public class FacilityIssueDto
{
    public Guid Id { get; set; }
    public Guid ReportedByMemberId { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;

    public Guid? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentSerialNumber { get; set; }

    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationFloor { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SanitizedDescription { get; set; }
    public string? ResolutionNotes { get; set; }
    public string ModerationStatus { get; set; } = "Clean";
    public string? ModerationReason { get; set; }
    public IssueSeverity Severity { get; set; }
    public string SeverityName => Severity.ToString();
    public IssueUrgencyLevel Urgency { get; set; } = IssueUrgencyLevel.Normal;
    public string UrgencyName => Urgency.ToString();
    public string? EmergencyEscalationContact { get; set; }
    public DateTime? TargetResolutionTime { get; set; }
    public bool IsOverdue => Status != FacilityIssueStatus.Resolved && TargetResolutionTime.HasValue && DateTime.UtcNow > TargetResolutionTime.Value;
    public FacilityIssueStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<IssueImageDto> Images { get; set; } = Array.Empty<IssueImageDto>();
    public int RepairOrdersCount { get; set; }
}

public class FacilityIssueSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
    public IssueUrgencyLevel Urgency { get; set; } = IssueUrgencyLevel.Normal;
    public string UrgencyName => Urgency.ToString();
    public DateTime? TargetResolutionTime { get; set; }
    public bool IsOverdue => Status != FacilityIssueStatus.Resolved && TargetResolutionTime.HasValue && DateTime.UtcNow > TargetResolutionTime.Value;
    public FacilityIssueStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class IssueImageDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? OriginalFileName { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CreateFacilityIssueRequest
{
    [Required]
    public Guid LocationId { get; set; }

    public Guid? EquipmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;

    public IssueUrgencyLevel Urgency { get; set; } = IssueUrgencyLevel.Normal;

    [MaxLength(100)]
    public string? EmergencyEscalationContact { get; set; }
}

public class UpdateFacilityIssueRequest
{
    [Required]
    public Guid LocationId { get; set; }

    public Guid? EquipmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;

    public IssueUrgencyLevel Urgency { get; set; } = IssueUrgencyLevel.Normal;

    [MaxLength(100)]
    public string? EmergencyEscalationContact { get; set; }
}

public class EscalateIssueRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public IssueUrgencyLevel Urgency { get; set; } = IssueUrgencyLevel.CriticalEmergency;

    [MaxLength(100)]
    public string? EscalationContact { get; set; }
}

public class BatchUpdateIssueStatusRequest
{
    [Required]
    public List<Guid> IssueIds { get; set; } = new();

    [Required]
    public FacilityIssueStatus NewStatus { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class IssueStatusTransitionRequest
{
    [Required]
    public FacilityIssueStatus NewStatus { get; set; }

    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }

    public decimal? EstimatedCost { get; set; }
}

public class FacilityIssueQueryParameters : PagedRequest
{
    public FacilityIssueStatus? Status { get; set; }
    public IssueSeverity? Severity { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? MemberId { get; set; }
}

public class IssueHistoryDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    public string? Notes { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime Timestamp { get; set; }
}
#endregion

#region Repair Order DTOs
public class RepairOrderDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string IssueTitle { get; set; } = string.Empty;
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public RepairOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? TechnicianName { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public bool RequiresApproval { get; set; }
    public ApprovalDto? Approval { get; set; }
    public IReadOnlyList<RepairOrderItemDto> Items { get; set; } = Array.Empty<RepairOrderItemDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RepairOrderItemDto
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class CreateRepairOrderItemRequest
{
    [Required]
    [MaxLength(150)]
    public string PartName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PartNumber { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Range(0, 1000000)]
    public decimal UnitCost { get; set; }
}

public class CreateRepairOrderRequest
{
    [Required]
    public Guid IssueId { get; set; }

    [Required]
    public Guid EquipmentId { get; set; }

    [MaxLength(50)]
    public string? OrderNumber { get; set; }

    [Range(0, 5000000)]
    public decimal EstimatedCost { get; set; }

    [MaxLength(150)]
    public string? TechnicianName { get; set; }

    public Guid? SupplierId { get; set; }

    public List<CreateRepairOrderItemRequest> Items { get; set; } = new();
}

public class UpdateRepairOrderRequest
{
    [Range(0, 5000000)]
    public decimal EstimatedCost { get; set; }

    public decimal? ActualCost { get; set; }

    public RepairOrderStatus Status { get; set; }

    [MaxLength(150)]
    public string? TechnicianName { get; set; }

    public Guid? SupplierId { get; set; }

    public List<CreateRepairOrderItemRequest>? Items { get; set; }
}

public class ApprovalDto
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public ApprovalDecision Decision { get; set; }
    public string DecisionName => Decision.ToString();
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
    public decimal ApprovalThreshold { get; set; }
    public decimal EstimatedCost { get; set; }
}

public class ProcessApprovalRequest
{
    [Required]
    public ApprovalDecision Decision { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }
}
#endregion

#region Feedback DTOs
public class FeedbackDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public FeedbackStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? AdminResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFeedbackRequest
{
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;
}

public class RespondFeedbackRequest
{
    [Required]
    [MaxLength(2000)]
    public string AdminResponse { get; set; } = string.Empty;

    public FeedbackStatus? Status { get; set; } = FeedbackStatus.Reviewed;
}
#endregion

#region Content Moderation
public class ContentModerationResult
{
    public bool IsFlagged { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string SanitizedText { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = "Clean"; // Clean, Flagged, Masked
    public string? ModerationReason { get; set; }
    public List<string> DetectedKeywords { get; set; } = new();
}
#endregion
