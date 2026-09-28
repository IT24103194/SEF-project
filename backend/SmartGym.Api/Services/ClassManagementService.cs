using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Classes;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.Entities;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Services;

public class ClassManagementService : IClassManagementService
{
    private readonly SmartGymDbContext _context;
    private readonly ILogger<ClassManagementService> _logger;

    public ClassManagementService(SmartGymDbContext context, ILogger<ClassManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==========================================
    // CLASS CATEGORIES
    // ==========================================

    public async Task<List<ClassCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ClassCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClassCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                FitnessClassCount = c.FitnessClasses.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassCategoryDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.ClassCategories
            .AsNoTracking()
            .Include(c => c.FitnessClasses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class category with ID '{id}' was not found.");

        return new ClassCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            FitnessClassCount = category.FitnessClasses.Count,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<ClassCategoryDto> CreateCategoryAsync(CreateClassCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        var exists = await _context.ClassCategories
            .AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (exists)
            throw new InvalidOperationException($"A class category named '{trimmedName}' already exists.");

        var category = new ClassCategory
        {
            Name = trimmedName,
            Description = request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.ClassCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClassCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            FitnessClassCount = 0,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<ClassCategoryDto> UpdateCategoryAsync(Guid id, UpdateClassCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _context.ClassCategories
            .Include(c => c.FitnessClasses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class category with ID '{id}' was not found.");

        var trimmedName = request.Name.Trim();
        var duplicate = await _context.ClassCategories
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicate)
            throw new InvalidOperationException($"Another category named '{trimmedName}' already exists.");

        category.Name = trimmedName;
        category.Description = request.Description.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new ClassCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            FitnessClassCount = category.FitnessClasses.Count,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.ClassCategories
            .Include(c => c.FitnessClasses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class category with ID '{id}' was not found.");

        if (category.FitnessClasses.Any())
            throw new InvalidOperationException($"Cannot delete category '{category.Name}' because {category.FitnessClasses.Count} fitness classes are assigned to it.");

        _context.ClassCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // FITNESS CLASSES
    // ==========================================

    public async Task<PagedResult<FitnessClassDto>> GetClassesAsync(
        string? search,
        Guid? categoryId,
        string? intensity,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.FitnessClasses
            .AsNoTracking()
            .Include(fc => fc.Category)
            .Include(fc => fc.Schedules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(fc => fc.Name.ToLower().Contains(term) || fc.Description.ToLower().Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(fc => fc.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(intensity))
        {
            var lvl = intensity.Trim().ToLower();
            query = query.Where(fc => fc.IntensityLevel.ToLower() == lvl);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(fc => fc.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(fc => new FitnessClassDto
            {
                Id = fc.Id,
                CategoryId = fc.CategoryId,
                CategoryName = fc.Category.Name,
                Name = fc.Name,
                Description = fc.Description,
                DurationMinutes = fc.DurationMinutes,
                DefaultCapacity = fc.DefaultCapacity,
                IntensityLevel = fc.IntensityLevel,
                TotalSchedulesCount = fc.Schedules.Count,
                CreatedAt = fc.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<FitnessClassDto>.Create(items, total, page, pageSize);
    }

    public async Task<FitnessClassDto> GetClassByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fc = await _context.FitnessClasses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness class with ID '{id}' was not found.");

        return new FitnessClassDto
        {
            Id = fc.Id,
            CategoryId = fc.CategoryId,
            CategoryName = fc.Category.Name,
            Name = fc.Name,
            Description = fc.Description,
            DurationMinutes = fc.DurationMinutes,
            DefaultCapacity = fc.DefaultCapacity,
            IntensityLevel = fc.IntensityLevel,
            TotalSchedulesCount = fc.Schedules.Count,
            CreatedAt = fc.CreatedAt
        };
    }

    public async Task<FitnessClassDto> CreateClassAsync(CreateFitnessClassRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _context.ClassCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Class category with ID '{request.CategoryId}' was not found.");

        var trimmedName = request.Name.Trim();
        var exists = await _context.FitnessClasses
            .AnyAsync(fc => fc.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (exists)
            throw new InvalidOperationException($"A fitness class named '{trimmedName}' already exists.");

        var fitnessClass = new FitnessClass
        {
            CategoryId = category.Id,
            Name = trimmedName,
            Description = request.Description.Trim(),
            DurationMinutes = request.DurationMinutes,
            DefaultCapacity = request.DefaultCapacity,
            IntensityLevel = request.IntensityLevel.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.FitnessClasses.AddAsync(fitnessClass, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new FitnessClassDto
        {
            Id = fitnessClass.Id,
            CategoryId = fitnessClass.CategoryId,
            CategoryName = category.Name,
            Name = fitnessClass.Name,
            Description = fitnessClass.Description,
            DurationMinutes = fitnessClass.DurationMinutes,
            DefaultCapacity = fitnessClass.DefaultCapacity,
            IntensityLevel = fitnessClass.IntensityLevel,
            TotalSchedulesCount = 0,
            CreatedAt = fitnessClass.CreatedAt
        };
    }

    public async Task<FitnessClassDto> UpdateClassAsync(Guid id, UpdateFitnessClassRequest request, CancellationToken cancellationToken = default)
    {
        var fitnessClass = await _context.FitnessClasses
            .Include(fc => fc.Category)
            .Include(fc => fc.Schedules)
            .FirstOrDefaultAsync(fc => fc.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness class with ID '{id}' was not found.");

        var category = await _context.ClassCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Class category with ID '{request.CategoryId}' was not found.");

        var trimmedName = request.Name.Trim();
        var duplicate = await _context.FitnessClasses
            .AnyAsync(fc => fc.Id != id && fc.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicate)
            throw new InvalidOperationException($"Another fitness class named '{trimmedName}' already exists.");

        fitnessClass.CategoryId = category.Id;
        fitnessClass.Name = trimmedName;
        fitnessClass.Description = request.Description.Trim();
        fitnessClass.DurationMinutes = request.DurationMinutes;
        fitnessClass.DefaultCapacity = request.DefaultCapacity;
        fitnessClass.IntensityLevel = request.IntensityLevel.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new FitnessClassDto
        {
            Id = fitnessClass.Id,
            CategoryId = fitnessClass.CategoryId,
            CategoryName = category.Name,
            Name = fitnessClass.Name,
            Description = fitnessClass.Description,
            DurationMinutes = fitnessClass.DurationMinutes,
            DefaultCapacity = fitnessClass.DefaultCapacity,
            IntensityLevel = fitnessClass.IntensityLevel,
            TotalSchedulesCount = fitnessClass.Schedules.Count,
            CreatedAt = fitnessClass.CreatedAt
        };
    }

    public async Task DeleteClassAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fitnessClass = await _context.FitnessClasses
            .Include(fc => fc.Schedules)
            .FirstOrDefaultAsync(fc => fc.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness class with ID '{id}' was not found.");

        if (fitnessClass.Schedules.Any(s => s.Status == ScheduleStatus.Scheduled && s.StartTime >= DateTime.UtcNow))
            throw new InvalidOperationException($"Cannot delete class '{fitnessClass.Name}' because it has upcoming active schedules.");

        _context.FitnessClasses.Remove(fitnessClass);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // CLASS SCHEDULES
    // ==========================================

    public async Task<PagedResult<ClassScheduleDto>> GetSchedulesAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? classId,
        Guid? trainerId,
        ScheduleStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.FitnessClass).ThenInclude(fc => fc.Category)
            .Include(s => s.Trainer)
            .AsQueryable();

        if (startDate.HasValue)
        {
            var startUtc = startDate.Value.ToUniversalTime();
            query = query.Where(s => s.StartTime >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = endDate.Value.ToUniversalTime();
            query = query.Where(s => s.StartTime <= endUtc);
        }

        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        if (trainerId.HasValue)
            query = query.Where(s => s.TrainerId == trainerId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ClassScheduleDto
            {
                Id = s.Id,
                ClassId = s.ClassId,
                ClassName = s.FitnessClass.Name,
                CategoryName = s.FitnessClass.Category.Name,
                DurationMinutes = s.FitnessClass.DurationMinutes,
                IntensityLevel = s.FitnessClass.IntensityLevel,
                TrainerId = s.TrainerId,
                TrainerName = $"{s.Trainer.FirstName} {s.Trainer.LastName}",
                Room = s.Room,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Capacity = s.Capacity,
                BookedCount = s.BookedCount,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<ClassScheduleDto>.Create(items, total, page, pageSize);
    }

    public async Task<ClassScheduleDto> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await _context.ClassSchedules
            .AsNoTracking()
            .Include(x => x.FitnessClass).ThenInclude(fc => fc.Category)
            .Include(x => x.Trainer)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class schedule with ID '{id}' was not found.");

        return new ClassScheduleDto
        {
            Id = s.Id,
            ClassId = s.ClassId,
            ClassName = s.FitnessClass.Name,
            CategoryName = s.FitnessClass.Category.Name,
            DurationMinutes = s.FitnessClass.DurationMinutes,
            IntensityLevel = s.FitnessClass.IntensityLevel,
            TrainerId = s.TrainerId,
            TrainerName = $"{s.Trainer.FirstName} {s.Trainer.LastName}",
            Room = s.Room,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Capacity = s.Capacity,
            BookedCount = s.BookedCount,
            Status = s.Status,
            CreatedAt = s.CreatedAt
        };
    }

    public async Task<List<ClassScheduleDto>> GetTrainerSchedulesAsync(
        Guid trainerId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.FitnessClass).ThenInclude(fc => fc.Category)
            .Include(s => s.Trainer)
            .Where(s => s.TrainerId == trainerId);

        if (startDate.HasValue)
            query = query.Where(s => s.StartTime >= startDate.Value.ToUniversalTime());

        if (endDate.HasValue)
            query = query.Where(s => s.StartTime <= endDate.Value.ToUniversalTime());

        return await query
            .OrderBy(s => s.StartTime)
            .Select(s => new ClassScheduleDto
            {
                Id = s.Id,
                ClassId = s.ClassId,
                ClassName = s.FitnessClass.Name,
                CategoryName = s.FitnessClass.Category.Name,
                DurationMinutes = s.FitnessClass.DurationMinutes,
                IntensityLevel = s.FitnessClass.IntensityLevel,
                TrainerId = s.TrainerId,
                TrainerName = $"{s.Trainer.FirstName} {s.Trainer.LastName}",
                Room = s.Room,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Capacity = s.Capacity,
                BookedCount = s.BookedCount,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduleAvailabilityDto> GetScheduleAvailabilityAsync(
        Guid scheduleId,
        Guid? memberId,
        CancellationToken cancellationToken = default)
    {
        var s = await _context.ClassSchedules
            .AsNoTracking()
            .Include(x => x.FitnessClass)
            .FirstOrDefaultAsync(x => x.Id == scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Class schedule with ID '{scheduleId}' was not found.");

        var isFull = s.BookedCount >= s.Capacity;
        bool canBook = !isFull && s.Status == ScheduleStatus.Scheduled && s.StartTime > DateTime.UtcNow;
        string? reason = null;

        if (s.Status != ScheduleStatus.Scheduled)
        {
            canBook = false;
            reason = $"Class is {s.Status}.";
        }
        else if (s.StartTime <= DateTime.UtcNow)
        {
            canBook = false;
            reason = "Class has already started or passed.";
        }
        else if (isFull)
        {
            canBook = false;
            reason = "Class is at full capacity.";
        }
        else if (memberId.HasValue)
        {
            // Check duplicate booking
            var alreadyBooked = await _context.Bookings
                .AnyAsync(b => b.ScheduleId == scheduleId && b.MemberId == memberId.Value && b.Status == BookingStatus.Confirmed, cancellationToken);

            if (alreadyBooked)
            {
                canBook = false;
                reason = "Member already has a confirmed booking for this class.";
            }
            else
            {
                // Check membership
                var hasActiveMembership = await _context.Memberships
                    .AnyAsync(m => m.MemberId == memberId.Value &&
                                   m.Status == MembershipStatus.Active &&
                                   m.StartDate <= s.StartTime &&
                                   m.EndDate >= s.StartTime, cancellationToken);

                if (!hasActiveMembership)
                {
                    canBook = false;
                    reason = "Member does not hold an active membership for the scheduled date.";
                }
            }
        }

        return new ScheduleAvailabilityDto
        {
            ScheduleId = s.Id,
            ClassName = s.FitnessClass.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Capacity = s.Capacity,
            BookedCount = s.BookedCount,
            CanBook = canBook,
            ReasonCannotBook = reason
        };
    }

    public async Task<ClassScheduleDto> CreateScheduleAsync(CreateClassScheduleRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Validate dates
        var startUtc = request.StartTime.ToUniversalTime();
        var endUtc = request.EndTime.ToUniversalTime();

        if (endUtc <= startUtc)
            throw new ArgumentException("Schedule EndTime must be after StartTime.");

        if (startUtc < DateTime.UtcNow.AddMinutes(-5))
            throw new ArgumentException("Schedule StartTime cannot be set in the past.");

        // 2. Validate Class
        var fitnessClass = await _context.FitnessClasses
            .Include(fc => fc.Category)
            .FirstOrDefaultAsync(fc => fc.Id == request.ClassId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness class with ID '{request.ClassId}' was not found.");

        // 3. Validate Trainer
        var trainer = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.TrainerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Trainer with ID '{request.TrainerId}' was not found.");

        var isTrainer = trainer.UserRoles.Any(ur => ur.Role.Name.Equals("Trainer", StringComparison.OrdinalIgnoreCase) || ur.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        if (!isTrainer)
            throw new InvalidOperationException($"User '{trainer.FirstName} {trainer.LastName}' is not assigned the Trainer or Admin role.");

        // 4. Validate Trainer Schedule Conflict (Business Rule 6)
        var conflict = await _context.ClassSchedules
            .Include(s => s.FitnessClass)
            .FirstOrDefaultAsync(s =>
                s.TrainerId == request.TrainerId &&
                s.Status != ScheduleStatus.Cancelled &&
                s.StartTime < endUtc && startUtc < s.EndTime, cancellationToken);

        if (conflict != null)
        {
            throw new InvalidOperationException(
                $"Trainer {trainer.FirstName} {trainer.LastName} has a scheduling conflict with '{conflict.FitnessClass.Name}' ({conflict.StartTime:g} - {conflict.EndTime:t}).");
        }

        var schedule = new ClassSchedule
        {
            ClassId = fitnessClass.Id,
            TrainerId = trainer.Id,
            Room = request.Room.Trim(),
            StartTime = startUtc,
            EndTime = endUtc,
            Capacity = request.Capacity > 0 ? request.Capacity : fitnessClass.DefaultCapacity,
            BookedCount = 0,
            Status = ScheduleStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ClassSchedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClassScheduleDto
        {
            Id = schedule.Id,
            ClassId = schedule.ClassId,
            ClassName = fitnessClass.Name,
            CategoryName = fitnessClass.Category.Name,
            DurationMinutes = fitnessClass.DurationMinutes,
            IntensityLevel = fitnessClass.IntensityLevel,
            TrainerId = trainer.Id,
            TrainerName = $"{trainer.FirstName} {trainer.LastName}",
            Room = schedule.Room,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Capacity = schedule.Capacity,
            BookedCount = 0,
            Status = schedule.Status,
            CreatedAt = schedule.CreatedAt
        };
    }

    public async Task<ClassScheduleDto> UpdateScheduleAsync(Guid id, UpdateClassScheduleRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.ClassSchedules
            .Include(s => s.FitnessClass).ThenInclude(fc => fc.Category)
            .Include(s => s.Trainer)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class schedule with ID '{id}' was not found.");

        var startUtc = request.StartTime.ToUniversalTime();
        var endUtc = request.EndTime.ToUniversalTime();

        if (endUtc <= startUtc)
            throw new ArgumentException("Schedule EndTime must be after StartTime.");

        // Check trainer conflict
        var conflict = await _context.ClassSchedules
            .Include(s => s.FitnessClass)
            .FirstOrDefaultAsync(s =>
                s.Id != id &&
                s.TrainerId == request.TrainerId &&
                s.Status != ScheduleStatus.Cancelled &&
                s.StartTime < endUtc && startUtc < s.EndTime, cancellationToken);

        if (conflict != null)
        {
            throw new InvalidOperationException(
                $"Trainer has a scheduling conflict with '{conflict.FitnessClass.Name}' during the requested time.");
        }

        var fitnessClass = await _context.FitnessClasses
            .Include(fc => fc.Category)
            .FirstOrDefaultAsync(fc => fc.Id == request.ClassId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness class with ID '{request.ClassId}' was not found.");

        var trainer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.TrainerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Trainer with ID '{request.TrainerId}' was not found.");

        schedule.ClassId = fitnessClass.Id;
        schedule.TrainerId = trainer.Id;
        schedule.Room = request.Room.Trim();
        schedule.StartTime = startUtc;
        schedule.EndTime = endUtc;
        schedule.Capacity = request.Capacity;
        schedule.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return new ClassScheduleDto
        {
            Id = schedule.Id,
            ClassId = schedule.ClassId,
            ClassName = fitnessClass.Name,
            CategoryName = fitnessClass.Category.Name,
            DurationMinutes = fitnessClass.DurationMinutes,
            IntensityLevel = fitnessClass.IntensityLevel,
            TrainerId = trainer.Id,
            TrainerName = $"{trainer.FirstName} {trainer.LastName}",
            Room = schedule.Room,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Capacity = schedule.Capacity,
            BookedCount = schedule.BookedCount,
            Status = schedule.Status,
            CreatedAt = schedule.CreatedAt
        };
    }

    public async Task<ClassScheduleDto> CancelScheduleAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.ClassSchedules
            .Include(s => s.FitnessClass).ThenInclude(fc => fc.Category)
            .Include(s => s.Trainer)
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class schedule with ID '{id}' was not found.");

        schedule.Status = ScheduleStatus.Cancelled;

        // Cancel all confirmed bookings for this schedule
        foreach (var booking in schedule.Bookings.Where(b => b.Status == BookingStatus.Confirmed))
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = "Class schedule was cancelled by gym management.";
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ClassScheduleDto
        {
            Id = schedule.Id,
            ClassId = schedule.ClassId,
            ClassName = schedule.FitnessClass.Name,
            CategoryName = schedule.FitnessClass.Category.Name,
            DurationMinutes = schedule.FitnessClass.DurationMinutes,
            IntensityLevel = schedule.FitnessClass.IntensityLevel,
            TrainerId = schedule.TrainerId,
            TrainerName = $"{schedule.Trainer.FirstName} {schedule.Trainer.LastName}",
            Room = schedule.Room,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Capacity = schedule.Capacity,
            BookedCount = schedule.BookedCount,
            Status = schedule.Status,
            CreatedAt = schedule.CreatedAt
        };
    }

    public async Task DeleteScheduleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.ClassSchedules
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Class schedule with ID '{id}' was not found.");

        if (schedule.Bookings.Any(b => b.Status == BookingStatus.Confirmed))
            throw new InvalidOperationException("Cannot delete schedule that already contains active bookings. Cancel the schedule instead.");

        _context.ClassSchedules.Remove(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // BOOKINGS
    // ==========================================

    public async Task<PagedResult<BookingDto>> GetBookingsAsync(
        Guid? scheduleId,
        Guid? memberId,
        BookingStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Schedule).ThenInclude(s => s.FitnessClass)
            .Include(b => b.Schedule).ThenInclude(s => s.Trainer)
            .Include(b => b.Member).ThenInclude(m => m.User)
            .Include(b => b.Attendance)
            .AsQueryable();

        if (scheduleId.HasValue)
            query = query.Where(b => b.ScheduleId == scheduleId.Value);

        if (memberId.HasValue)
            query = query.Where(b => b.MemberId == memberId.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.BookingTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                ScheduleId = b.ScheduleId,
                ClassName = b.Schedule.FitnessClass.Name,
                TrainerName = $"{b.Schedule.Trainer.FirstName} {b.Schedule.Trainer.LastName}",
                Room = b.Schedule.Room,
                ClassStartTime = b.Schedule.StartTime,
                ClassEndTime = b.Schedule.EndTime,
                MemberId = b.MemberId,
                MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                MemberEmail = b.Member.User.Email,
                BookingTime = b.BookingTime,
                Status = b.Status,
                CancelledAt = b.CancelledAt,
                CancellationReason = b.CancellationReason,
                Attendance = b.Attendance == null ? null : new AttendanceDto
                {
                    Id = b.Attendance.Id,
                    ScheduleId = b.Attendance.ScheduleId,
                    BookingId = b.Attendance.BookingId,
                    MemberId = b.Attendance.MemberId,
                    MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                    CheckedInAt = b.Attendance.CheckedInAt,
                    Status = b.Attendance.Status,
                    MarkedByUserId = b.Attendance.MarkedByUserId
                }
            })
            .ToListAsync(cancellationToken);

        return PagedResult<BookingDto>.Create(items, total, page, pageSize);
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var b = await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Schedule).ThenInclude(s => s.FitnessClass)
            .Include(x => x.Schedule).ThenInclude(s => s.Trainer)
            .Include(x => x.Member).ThenInclude(m => m.User)
            .Include(x => x.Attendance)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking with ID '{id}' was not found.");

        if (!isStaff && b.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to view this booking.");

        return new BookingDto
        {
            Id = b.Id,
            ScheduleId = b.ScheduleId,
            ClassName = b.Schedule.FitnessClass.Name,
            TrainerName = $"{b.Schedule.Trainer.FirstName} {b.Schedule.Trainer.LastName}",
            Room = b.Schedule.Room,
            ClassStartTime = b.Schedule.StartTime,
            ClassEndTime = b.Schedule.EndTime,
            MemberId = b.MemberId,
            MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
            MemberEmail = b.Member.User.Email,
            BookingTime = b.BookingTime,
            Status = b.Status,
            CancelledAt = b.CancelledAt,
            CancellationReason = b.CancellationReason,
            Attendance = b.Attendance == null ? null : new AttendanceDto
            {
                Id = b.Attendance.Id,
                ScheduleId = b.Attendance.ScheduleId,
                BookingId = b.Attendance.BookingId,
                MemberId = b.Attendance.MemberId,
                MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                CheckedInAt = b.Attendance.CheckedInAt,
                Status = b.Attendance.Status,
                MarkedByUserId = b.Attendance.MarkedByUserId
            }
        };
    }

    public async Task<List<BookingDto>> GetMemberBookingsAsync(Guid memberId, bool? upcomingOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Schedule).ThenInclude(s => s.FitnessClass)
            .Include(b => b.Schedule).ThenInclude(s => s.Trainer)
            .Include(b => b.Member).ThenInclude(m => m.User)
            .Include(b => b.Attendance)
            .Where(b => b.MemberId == memberId);

        if (upcomingOnly == true)
        {
            query = query.Where(b => b.Schedule.StartTime >= DateTime.UtcNow && b.Status == BookingStatus.Confirmed);
        }

        return await query
            .OrderByDescending(b => b.Schedule.StartTime)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                ScheduleId = b.ScheduleId,
                ClassName = b.Schedule.FitnessClass.Name,
                TrainerName = $"{b.Schedule.Trainer.FirstName} {b.Schedule.Trainer.LastName}",
                Room = b.Schedule.Room,
                ClassStartTime = b.Schedule.StartTime,
                ClassEndTime = b.Schedule.EndTime,
                MemberId = b.MemberId,
                MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                MemberEmail = b.Member.User.Email,
                BookingTime = b.BookingTime,
                Status = b.Status,
                CancelledAt = b.CancelledAt,
                CancellationReason = b.CancellationReason,
                Attendance = b.Attendance == null ? null : new AttendanceDto
                {
                    Id = b.Attendance.Id,
                    ScheduleId = b.Attendance.ScheduleId,
                    BookingId = b.Attendance.BookingId,
                    MemberId = b.Attendance.MemberId,
                    MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                    CheckedInAt = b.Attendance.CheckedInAt,
                    Status = b.Attendance.Status,
                    MarkedByUserId = b.Attendance.MarkedByUserId
                }
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingDto> BookClassAsync(CreateBookingRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        // 1. Resolve Member
        Guid memberId;
        if (isStaff && request.MemberId.HasValue)
        {
            memberId = request.MemberId.Value;
        }
        else
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUserId, cancellationToken)
                ?? throw new KeyNotFoundException("Member profile not found for the current authenticated user.");
            memberId = member.Id;
        }

        var memberEntity = await _context.Members
            .Include(m => m.User)
            .Include(m => m.Memberships)
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with ID '{memberId}' was not found.");

        // 2. Load Schedule with tracking
        var schedule = await _context.ClassSchedules
            .Include(s => s.FitnessClass)
            .Include(s => s.Trainer)
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Class schedule with ID '{request.ScheduleId}' was not found.");

        // 3. Validate Schedule Status & Time
        if (schedule.Status != ScheduleStatus.Scheduled)
            throw new InvalidOperationException($"Cannot book class: schedule is marked as '{schedule.Status}'.");

        if (schedule.StartTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot book class: The class schedule has already started or completed.");

        // 4. Business Rule 1 & 2: Expired / Inactive membership check
        var hasValidMembership = memberEntity.Memberships.Any(sub =>
            sub.Status == MembershipStatus.Active &&
            sub.StartDate <= schedule.StartTime.Date &&
            sub.EndDate >= schedule.StartTime.Date);

        if (!hasValidMembership)
        {
            throw new InvalidOperationException(
                "Cannot book class: Member does not hold an active, non-expired membership valid for the scheduled class date.");
        }

        // 5. Business Rule 3: Duplicate booking prohibition
        var isDuplicate = schedule.Bookings.Any(b => b.MemberId == memberId && b.Status == BookingStatus.Confirmed);
        if (isDuplicate)
        {
            throw new InvalidOperationException(
                "Duplicate booking prohibited: You already have an active confirmed reservation for this class schedule.");
        }

        // 6. Business Rule 4: Full class capacity check
        if (schedule.BookedCount >= schedule.Capacity)
        {
            throw new InvalidOperationException(
                $"Cannot book class: This class schedule is full ({schedule.BookedCount}/{schedule.Capacity} spots taken).");
        }

        // 7. Increment booked count & persist reservation
        schedule.BookedCount++;

        var booking = new Booking
        {
            ScheduleId = schedule.Id,
            MemberId = memberId,
            BookingTime = DateTime.UtcNow,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Bookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Confirmed booking '{BookingId}' for member '{MemberId}' in class schedule '{ScheduleId}'.",
            booking.Id, memberId, schedule.Id);

        return new BookingDto
        {
            Id = booking.Id,
            ScheduleId = schedule.Id,
            ClassName = schedule.FitnessClass.Name,
            TrainerName = $"{schedule.Trainer.FirstName} {schedule.Trainer.LastName}",
            Room = schedule.Room,
            ClassStartTime = schedule.StartTime,
            ClassEndTime = schedule.EndTime,
            MemberId = memberEntity.Id,
            MemberName = $"{memberEntity.User.FirstName} {memberEntity.User.LastName}",
            MemberEmail = memberEntity.User.Email,
            BookingTime = booking.BookingTime,
            Status = booking.Status
        };
    }

    public async Task<BookingDto> CancelBookingAsync(
        Guid bookingId,
        CancelBookingRequest request,
        Guid currentUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Schedule).ThenInclude(s => s.FitnessClass)
            .Include(b => b.Schedule).ThenInclude(s => s.Trainer)
            .Include(b => b.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking with ID '{bookingId}' was not found.");

        // Authorization check: non-staff can only cancel their own booking
        if (!isStaff && booking.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to cancel this booking.");

        // Business Rule 7: Cancellation policy
        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot cancel booking that is already '{booking.Status}'.");

        if (booking.Schedule.StartTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Cancellation policy violation: Cannot cancel a class that has already started or ended.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Member requested cancellation"
            : request.Reason.Trim();

        // Decrement schedule booked count safely
        booking.Schedule.BookedCount = Math.Max(0, booking.Schedule.BookedCount - 1);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled booking '{BookingId}'. Spots remaining: {SpotsRemaining}",
            bookingId, booking.Schedule.Capacity - booking.Schedule.BookedCount);

        return new BookingDto
        {
            Id = booking.Id,
            ScheduleId = booking.ScheduleId,
            ClassName = booking.Schedule.FitnessClass.Name,
            TrainerName = $"{booking.Schedule.Trainer.FirstName} {booking.Schedule.Trainer.LastName}",
            Room = booking.Schedule.Room,
            ClassStartTime = booking.Schedule.StartTime,
            ClassEndTime = booking.Schedule.EndTime,
            MemberId = booking.MemberId,
            MemberName = $"{booking.Member.User.FirstName} {booking.Member.User.LastName}",
            MemberEmail = booking.Member.User.Email,
            BookingTime = booking.BookingTime,
            Status = booking.Status,
            CancelledAt = booking.CancelledAt,
            CancellationReason = booking.CancellationReason
        };
    }

    // ==========================================
    // ATTENDANCE
    // ==========================================

    public async Task<List<AttendanceDto>> GetAttendancesAsync(
        Guid? scheduleId,
        Guid? memberId,
        AttendanceStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Member).ThenInclude(m => m.User)
            .AsQueryable();

        if (scheduleId.HasValue)
            query = query.Where(a => a.ScheduleId == scheduleId.Value);

        if (memberId.HasValue)
            query = query.Where(a => a.MemberId == memberId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.CheckedInAt)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                ScheduleId = a.ScheduleId,
                BookingId = a.BookingId,
                MemberId = a.MemberId,
                MemberName = $"{a.Member.User.FirstName} {a.Member.User.LastName}",
                CheckedInAt = a.CheckedInAt,
                Status = a.Status,
                MarkedByUserId = a.MarkedByUserId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BookingDto>> GetScheduleAttendanceSheetAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Member).ThenInclude(m => m.User)
            .Include(b => b.Attendance)
            .Include(b => b.Schedule).ThenInclude(s => s.FitnessClass)
            .Include(b => b.Schedule).ThenInclude(s => s.Trainer)
            .Where(b => b.ScheduleId == scheduleId && b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.Member.User.LastName)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                ScheduleId = b.ScheduleId,
                ClassName = b.Schedule.FitnessClass.Name,
                TrainerName = $"{b.Schedule.Trainer.FirstName} {b.Schedule.Trainer.LastName}",
                Room = b.Schedule.Room,
                ClassStartTime = b.Schedule.StartTime,
                ClassEndTime = b.Schedule.EndTime,
                MemberId = b.MemberId,
                MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                MemberEmail = b.Member.User.Email,
                BookingTime = b.BookingTime,
                Status = b.Status,
                Attendance = b.Attendance == null ? null : new AttendanceDto
                {
                    Id = b.Attendance.Id,
                    ScheduleId = b.Attendance.ScheduleId,
                    BookingId = b.Attendance.BookingId,
                    MemberId = b.Attendance.MemberId,
                    MemberName = $"{b.Member.User.FirstName} {b.Member.User.LastName}",
                    CheckedInAt = b.Attendance.CheckedInAt,
                    Status = b.Attendance.Status,
                    MarkedByUserId = b.Attendance.MarkedByUserId
                }
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AttendanceDto> RecordAttendanceAsync(
        RecordAttendanceRequest request,
        Guid markedByUserId,
        CancellationToken cancellationToken = default)
    {
        // Business Rule 9: Attendance can only be recorded for legitimate bookings
        var booking = await _context.Bookings
            .Include(b => b.Member).ThenInclude(m => m.User)
            .Include(b => b.Schedule)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking with ID '{request.BookingId}' was not found.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Attendance can only be recorded for legitimate, confirmed bookings.");
        }

        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a => a.BookingId == request.BookingId, cancellationToken);

        if (existing != null)
        {
            existing.Status = request.Status;
            existing.MarkedByUserId = markedByUserId;
            existing.CheckedInAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new AttendanceDto
            {
                Id = existing.Id,
                ScheduleId = existing.ScheduleId,
                BookingId = existing.BookingId,
                MemberId = existing.MemberId,
                MemberName = $"{booking.Member.User.FirstName} {booking.Member.User.LastName}",
                CheckedInAt = existing.CheckedInAt,
                Status = existing.Status,
                MarkedByUserId = existing.MarkedByUserId
            };
        }

        var attendance = new Attendance
        {
            ScheduleId = booking.ScheduleId,
            BookingId = booking.Id,
            MemberId = booking.MemberId,
            CheckedInAt = DateTime.UtcNow,
            Status = request.Status,
            MarkedByUserId = markedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Attendances.AddAsync(attendance, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AttendanceDto
        {
            Id = attendance.Id,
            ScheduleId = attendance.ScheduleId,
            BookingId = attendance.BookingId,
            MemberId = attendance.MemberId,
            MemberName = $"{booking.Member.User.FirstName} {booking.Member.User.LastName}",
            CheckedInAt = attendance.CheckedInAt,
            Status = attendance.Status,
            MarkedByUserId = attendance.MarkedByUserId
        };
    }

    public async Task<List<AttendanceDto>> RecordBulkAttendanceAsync(
        BulkAttendanceRequest request,
        Guid markedByUserId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AttendanceDto>();
        foreach (var item in request.Records)
        {
            var res = await RecordAttendanceAsync(new RecordAttendanceRequest
            {
                BookingId = item.BookingId,
                Status = item.Status
            }, markedByUserId, cancellationToken);
            results.Add(res);
        }
        return results;
    }
}
