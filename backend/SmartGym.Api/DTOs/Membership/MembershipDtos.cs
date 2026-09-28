using System.ComponentModel.DataAnnotations;
using SmartGym.Api.Entities;

namespace SmartGym.Api.DTOs.Membership;

// --- Plan DTOs ---

public class MembershipPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxClassesPerWeek { get; set; }
    public bool HasTrainerAccess { get; set; }
    public bool IsActive { get; set; }
    public int ActiveSubscribersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMembershipPlanRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    public int DurationDays { get; set; } = 30;

    [Range(0, 50)]
    public int MaxClassesPerWeek { get; set; } = 5;

    public bool HasTrainerAccess { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class UpdateMembershipPlanRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    public int DurationDays { get; set; } = 30;

    [Range(0, 50)]
    public int MaxClassesPerWeek { get; set; } = 5;

    public bool HasTrainerAccess { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

// --- Member DTOs ---

public class MemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? MedicalConditions { get; set; }
    public DateTime JoinDate { get; set; }
    public MembershipDto? CurrentMembership { get; set; }
    public int ActiveGoalsCount { get; set; }
    public int TotalBookingsCount { get; set; }
}

public class UpdateMemberProfileRequest
{
    [MaxLength(150)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(50)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(1000)]
    public string? MedicalConditions { get; set; }
}

// --- Membership Subscription DTOs ---

public class MembershipDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MembershipStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public bool IsExpired => EndDate < DateTime.UtcNow || Status == MembershipStatus.Expired;
    public int DaysRemaining => Math.Max(0, (EndDate.Date - DateTime.UtcNow.Date).Days);
    public bool AutoRenew { get; set; }
    public decimal PricePaid { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMembershipRequest
{
    [Required]
    public Guid MemberId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    public DateTime? StartDate { get; set; }

    public bool AutoRenew { get; set; } = false;
}

public class RenewMembershipRequest
{
    public Guid? MemberId { get; set; } // Required if admin renewing for member

    [Required]
    public Guid PlanId { get; set; }

    public bool AutoRenew { get; set; } = false;
}

public class CancelMembershipRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; } = "Member requested cancellation";
}

// --- Goal DTOs ---

public class GoalDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = "kg";
    public DateTime TargetDate { get; set; }
    public GoalStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal ProgressPercentage => TargetValue == 0 ? 0 : Math.Clamp(Math.Round((CurrentValue / TargetValue) * 100, 1), 0, 100);
    public int TotalLogsCount { get; set; }
    public DateTime? LastLogDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProgressRecordDto> RecentLogs { get; set; } = new();
}

public class CreateGoalRequest
{
    public Guid? MemberId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0.01, 10000)]
    public decimal TargetValue { get; set; }

    [Range(0, 10000)]
    public decimal CurrentValue { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "kg";

    [Required]
    public DateTime TargetDate { get; set; }
}

public class UpdateGoalRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0.01, 10000)]
    public decimal TargetValue { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "kg";

    [Required]
    public DateTime TargetDate { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.InProgress;
}

// --- Progress Record DTOs ---

public class ProgressRecordDto
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }
    public string GoalTitle { get; set; } = string.Empty;
    public DateTime RecordedDate { get; set; }
    public decimal Value { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByTrainerId { get; set; }
    public string? RecordedByTrainerName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProgressRecordRequest
{
    [Required]
    public Guid GoalId { get; set; }

    [Range(0, 10000)]
    public decimal Value { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime? RecordedDate { get; set; }
}

// --- Analytics DTO ---

public class MembershipAnalyticsDto
{
    public int TotalMembers { get; set; }
    public int ActiveMemberships { get; set; }
    public int ExpiredMemberships { get; set; }
    public int CancelledMemberships { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalGoals { get; set; }
    public int AchievedGoals { get; set; }
    public decimal GoalCompletionRate => TotalGoals == 0 ? 0 : Math.Round(((decimal)AchievedGoals / TotalGoals) * 100, 1);
    public List<PlanDistributionItem> PlanDistribution { get; set; } = new();
}

public class PlanDistributionItem
{
    public string PlanName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}
