namespace SmartGym.Api.Entities;

public enum MembershipStatus
{
    Active = 1,
    Expired = 2,
    Cancelled = 3
}

public enum GoalStatus
{
    InProgress = 1,
    Achieved = 2,
    Abandoned = 3
}

public enum ScheduleStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3
}

public enum BookingStatus
{
    Confirmed = 1,
    Cancelled = 2,
    Waitlisted = 3
}

public enum AttendanceStatus
{
    Attended = 1,
    Absent = 2,
    Excused = 3
}

public enum StockMovementType
{
    Restock = 1,
    Sale = 2,
    Adjustment = 3,
    Waste = 4,
    Return = 5
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Received = 4,
    Cancelled = 5
}

public enum EquipmentStatus
{
    Operational = 1,
    NeedsMaintenance = 2,
    OutOfService = 3,
    UnderRepair = 4
}

public enum FeedbackStatus
{
    Pending = 1,
    Reviewed = 2,
    Moderated = 3,
    Actioned = 4
}

public enum IssueSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum FacilityIssueStatus
{
    Reported = 1,
    InReview = 2,
    Diagnosing = 3,
    RequiresApproval = 4,
    InRepair = 5,
    Resolved = 6,
    Cancelled = 7
}

public enum RepairOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Ordered = 4,
    InProgress = 5,
    Completed = 6,
    Rejected = 7
}

public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2,
    Revised = 3
}

public enum AIWorkflowStatus
{
    Initiated = 1,
    Planning = 2,
    Validating = 3,
    Analyzing = 4,
    AwaitingApproval = 5,
    Executing = 6,
    Completed = 7,
    Failed = 8
}

public enum NotificationType
{
    General = 1,
    Booking = 2,
    Maintenance = 3,
    Approval = 4
}
