using System.ComponentModel.DataAnnotations;
using SmartGym.Api.Entities;

namespace SmartGym.Api.DTOs.Classes;

// --- Category DTOs ---

public class ClassCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int FitnessClassCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClassCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateClassCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

// --- Fitness Class DTOs ---

public class FitnessClassDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public string IntensityLevel { get; set; } = "Medium";
    public int TotalSchedulesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFitnessClassRequest
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(15, 300)]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 100)]
    public int DefaultCapacity { get; set; } = 20;

    [Required, MaxLength(50)]
    public string IntensityLevel { get; set; } = "Medium";
}

public class UpdateFitnessClassRequest
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(15, 300)]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 100)]
    public int DefaultCapacity { get; set; } = 20;

    [Required, MaxLength(50)]
    public string IntensityLevel { get; set; } = "Medium";
}

// --- Class Schedule DTOs ---

public class ClassScheduleDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string IntensityLevel { get; set; } = string.Empty;
    public Guid TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int AvailableSpots => Math.Max(0, Capacity - BookedCount);
    public bool IsFull => BookedCount >= Capacity;
    public ScheduleStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClassScheduleRequest
{
    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid TrainerId { get; set; }

    [Required, MaxLength(100)]
    public string Room { get; set; } = "Studio 1";

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Range(1, 100)]
    public int Capacity { get; set; } = 20;
}

public class UpdateClassScheduleRequest
{
    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid TrainerId { get; set; }

    [Required, MaxLength(100)]
    public string Room { get; set; } = "Studio 1";

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Range(1, 100)]
    public int Capacity { get; set; } = 20;

    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
}

public class ScheduleAvailabilityDto
{
    public Guid ScheduleId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int AvailableSpots => Math.Max(0, Capacity - BookedCount);
    public bool IsFull => BookedCount >= Capacity;
    public bool CanBook { get; set; }
    public string? ReasonCannotBook { get; set; }
}

// --- Booking DTOs ---

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DateTime ClassStartTime { get; set; }
    public DateTime ClassEndTime { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public DateTime BookingTime { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public AttendanceDto? Attendance { get; set; }
}

public class CreateBookingRequest
{
    [Required]
    public Guid ScheduleId { get; set; }

    public Guid? MemberId { get; set; } // If admin/trainer books for member; otherwise inferred from token
}

public class CancelBookingRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; } = "Member requested cancellation";
}

// --- Attendance DTOs ---

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid BookingId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime CheckedInAt { get; set; }
    public AttendanceStatus Status { get; set; }
    public Guid? MarkedByUserId { get; set; }
    public string? MarkedByUserName { get; set; }
}

public class RecordAttendanceRequest
{
    [Required]
    public Guid BookingId { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Attended;
}

public class BulkAttendanceRequest
{
    [Required]
    public Guid ScheduleId { get; set; }

    [Required]
    public List<AttendanceRecordItem> Records { get; set; } = new();
}

public class AttendanceRecordItem
{
    public Guid BookingId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Attended;
}
