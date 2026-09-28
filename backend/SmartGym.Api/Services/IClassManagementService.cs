using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface IClassManagementService
{
    // Categories
    Task<List<ClassCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ClassCategoryDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClassCategoryDto> CreateCategoryAsync(CreateClassCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ClassCategoryDto> UpdateCategoryAsync(Guid id, UpdateClassCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    // Fitness Classes
    Task<PagedResult<FitnessClassDto>> GetClassesAsync(string? search, Guid? categoryId, string? intensity, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FitnessClassDto> GetClassByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FitnessClassDto> CreateClassAsync(CreateFitnessClassRequest request, CancellationToken cancellationToken = default);
    Task<FitnessClassDto> UpdateClassAsync(Guid id, UpdateFitnessClassRequest request, CancellationToken cancellationToken = default);
    Task DeleteClassAsync(Guid id, CancellationToken cancellationToken = default);

    // Schedules
    Task<PagedResult<ClassScheduleDto>> GetSchedulesAsync(DateTime? startDate, DateTime? endDate, Guid? classId, Guid? trainerId, ScheduleStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ClassScheduleDto> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ClassScheduleDto>> GetTrainerSchedulesAsync(Guid trainerId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<ScheduleAvailabilityDto> GetScheduleAvailabilityAsync(Guid scheduleId, Guid? memberId, CancellationToken cancellationToken = default);
    Task<ClassScheduleDto> CreateScheduleAsync(CreateClassScheduleRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<ClassScheduleDto> UpdateScheduleAsync(Guid id, UpdateClassScheduleRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<ClassScheduleDto> CancelScheduleAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteScheduleAsync(Guid id, CancellationToken cancellationToken = default);

    // Bookings
    Task<PagedResult<BookingDto>> GetBookingsAsync(Guid? scheduleId, Guid? memberId, BookingStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BookingDto> GetBookingByIdAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<List<BookingDto>> GetMemberBookingsAsync(Guid memberId, bool? upcomingOnly, CancellationToken cancellationToken = default);
    Task<BookingDto> BookClassAsync(CreateBookingRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<BookingDto> CancelBookingAsync(Guid bookingId, CancelBookingRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);

    // Attendance
    Task<List<AttendanceDto>> GetAttendancesAsync(Guid? scheduleId, Guid? memberId, AttendanceStatus? status, CancellationToken cancellationToken = default);
    Task<List<BookingDto>> GetScheduleAttendanceSheetAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<AttendanceDto> RecordAttendanceAsync(RecordAttendanceRequest request, Guid markedByUserId, CancellationToken cancellationToken = default);
    Task<List<AttendanceDto>> RecordBulkAttendanceAsync(BulkAttendanceRequest request, Guid markedByUserId, CancellationToken cancellationToken = default);
}
