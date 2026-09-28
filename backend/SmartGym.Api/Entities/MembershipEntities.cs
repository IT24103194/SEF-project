namespace SmartGym.Api.Entities;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? MedicalConditions { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<FacilityIssue> ReportedIssues { get; set; } = new List<FacilityIssue>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

public class MembershipPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; } = 30;
    public int MaxClassesPerWeek { get; set; } = 5;
    public bool HasTrainerAccess { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

public class Membership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public Guid PlanId { get; set; }
    public MembershipPlan Plan { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public bool AutoRenew { get; set; } = false;
    public decimal PricePaid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = "kg";
    public DateTime TargetDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.InProgress;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
}

public class ProgressRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GoalId { get; set; }
    public Goal Goal { get; set; } = null!;

    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    public decimal Value { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByTrainerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
