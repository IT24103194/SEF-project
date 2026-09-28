using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Entities;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Services;

public class MembershipService : IMembershipService
{
    private readonly SmartGymDbContext _context;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(SmartGymDbContext context, ILogger<MembershipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==========================================
    // MEMBERSHIP PLANS
    // ==========================================

    public async Task<List<MembershipPlanDto>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.MembershipPlans
            .AsNoTracking()
            .Include(p => p.Memberships)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.Price)
            .Select(p => new MembershipPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                MaxClassesPerWeek = p.MaxClassesPerWeek,
                HasTrainerAccess = p.HasTrainerAccess,
                IsActive = p.IsActive,
                ActiveSubscribersCount = p.Memberships.Count(m => m.Status == MembershipStatus.Active && m.EndDate >= DateTime.UtcNow),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MembershipPlanDto> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _context.MembershipPlans
            .AsNoTracking()
            .Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Membership plan with ID '{id}' was not found.");

        return new MembershipPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            DurationDays = p.DurationDays,
            MaxClassesPerWeek = p.MaxClassesPerWeek,
            HasTrainerAccess = p.HasTrainerAccess,
            IsActive = p.IsActive,
            ActiveSubscribersCount = p.Memberships.Count(m => m.Status == MembershipStatus.Active && m.EndDate >= DateTime.UtcNow),
            CreatedAt = p.CreatedAt
        };
    }

    public async Task<MembershipPlanDto> CreatePlanAsync(CreateMembershipPlanRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        var exists = await _context.MembershipPlans
            .AnyAsync(p => p.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (exists)
            throw new InvalidOperationException($"A membership plan named '{trimmedName}' already exists.");

        var plan = new MembershipPlan
        {
            Name = trimmedName,
            Description = request.Description.Trim(),
            Price = request.Price,
            DurationDays = request.DurationDays,
            MaxClassesPerWeek = request.MaxClassesPerWeek,
            HasTrainerAccess = request.HasTrainerAccess,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _context.MembershipPlans.AddAsync(plan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new MembershipPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            DurationDays = plan.DurationDays,
            MaxClassesPerWeek = plan.MaxClassesPerWeek,
            HasTrainerAccess = plan.HasTrainerAccess,
            IsActive = plan.IsActive,
            ActiveSubscribersCount = 0,
            CreatedAt = plan.CreatedAt
        };
    }

    public async Task<MembershipPlanDto> UpdatePlanAsync(Guid id, UpdateMembershipPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _context.MembershipPlans
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Membership plan with ID '{id}' was not found.");

        var trimmedName = request.Name.Trim();
        var duplicate = await _context.MembershipPlans
            .AnyAsync(p => p.Id != id && p.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicate)
            throw new InvalidOperationException($"Another plan named '{trimmedName}' already exists.");

        plan.Name = trimmedName;
        plan.Description = request.Description.Trim();
        plan.Price = request.Price;
        plan.DurationDays = request.DurationDays;
        plan.MaxClassesPerWeek = request.MaxClassesPerWeek;
        plan.HasTrainerAccess = request.HasTrainerAccess;
        plan.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new MembershipPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            DurationDays = plan.DurationDays,
            MaxClassesPerWeek = plan.MaxClassesPerWeek,
            HasTrainerAccess = plan.HasTrainerAccess,
            IsActive = plan.IsActive,
            ActiveSubscribersCount = plan.Memberships.Count(m => m.Status == MembershipStatus.Active && m.EndDate >= DateTime.UtcNow),
            CreatedAt = plan.CreatedAt
        };
    }

    public async Task DeletePlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _context.MembershipPlans
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Membership plan with ID '{id}' was not found.");

        if (plan.Memberships.Any(m => m.Status == MembershipStatus.Active && m.EndDate >= DateTime.UtcNow))
            throw new InvalidOperationException($"Cannot delete plan '{plan.Name}' because active subscribers are enrolled. Deactivate the plan instead.");

        _context.MembershipPlans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // MEMBERS
    // ==========================================

    public async Task<PagedResult<MemberDto>> GetMembersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Members
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.Memberships).ThenInclude(ms => ms.Plan)
            .Include(m => m.Goals)
            .Include(m => m.Bookings)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.User.FirstName.ToLower().Contains(term) ||
                m.User.LastName.ToLower().Contains(term) ||
                m.User.Email.ToLower().Contains(term) ||
                (m.User.PhoneNumber != null && m.User.PhoneNumber.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.JoinDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                Email = m.User.Email,
                PhoneNumber = m.User.PhoneNumber ?? string.Empty,
                EmergencyContactName = m.EmergencyContactName,
                EmergencyContactPhone = m.EmergencyContactPhone,
                Gender = m.Gender,
                Address = m.Address,
                MedicalConditions = m.MedicalConditions,
                JoinDate = m.JoinDate,
                CurrentMembership = m.Memberships
                    .Where(sub => sub.Status == MembershipStatus.Active)
                    .OrderByDescending(sub => sub.EndDate)
                    .Select(sub => new MembershipDto
                    {
                        Id = sub.Id,
                        MemberId = sub.MemberId,
                        MemberName = $"{m.User.FirstName} {m.User.LastName}",
                        MemberEmail = m.User.Email,
                        PlanId = sub.PlanId,
                        PlanName = sub.Plan.Name,
                        StartDate = sub.StartDate,
                        EndDate = sub.EndDate,
                        Status = sub.Status,
                        AutoRenew = sub.AutoRenew,
                        PricePaid = sub.PricePaid,
                        CreatedAt = sub.CreatedAt
                    })
                    .FirstOrDefault(),
                ActiveGoalsCount = m.Goals.Count(g => g.Status == GoalStatus.InProgress),
                TotalBookingsCount = m.Bookings.Count
            })
            .ToListAsync(cancellationToken);

        return PagedResult<MemberDto>.Create(items, total, page, pageSize);
    }

    public async Task<MemberDto> GetMemberByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var m = await _context.Members
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Memberships).ThenInclude(ms => ms.Plan)
            .Include(x => x.Goals)
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with ID '{id}' was not found.");

        return MapToMemberDto(m);
    }

    public async Task<MemberDto> GetMemberByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var m = await _context.Members
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Memberships).ThenInclude(ms => ms.Plan)
            .Include(x => x.Goals)
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Member profile with user ID '{userId}' was not found.");

        return MapToMemberDto(m);
    }

    public async Task<MemberDto> UpdateMemberProfileAsync(Guid memberId, UpdateMemberProfileRequest request, CancellationToken cancellationToken = default)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .Include(m => m.Memberships).ThenInclude(ms => ms.Plan)
            .Include(m => m.Goals)
            .Include(m => m.Bookings)
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with ID '{memberId}' was not found.");

        member.EmergencyContactName = request.EmergencyContactName?.Trim();
        member.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        member.Gender = request.Gender?.Trim();
        member.Address = request.Address?.Trim();
        member.MedicalConditions = request.MedicalConditions?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return MapToMemberDto(member);
    }

    private static MemberDto MapToMemberDto(Member m)
    {
        var currentMembership = m.Memberships
            .Where(sub => sub.Status == MembershipStatus.Active)
            .OrderByDescending(sub => sub.EndDate)
            .Select(sub => new MembershipDto
            {
                Id = sub.Id,
                MemberId = sub.MemberId,
                MemberName = $"{m.User.FirstName} {m.User.LastName}",
                MemberEmail = m.User.Email,
                PlanId = sub.PlanId,
                PlanName = sub.Plan?.Name ?? "Plan",
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                Status = sub.Status,
                AutoRenew = sub.AutoRenew,
                PricePaid = sub.PricePaid,
                CreatedAt = sub.CreatedAt
            })
            .FirstOrDefault();

        return new MemberDto
        {
            Id = m.Id,
            UserId = m.UserId,
            FirstName = m.User.FirstName,
            LastName = m.User.LastName,
            Email = m.User.Email,
            PhoneNumber = m.User.PhoneNumber ?? string.Empty,
            EmergencyContactName = m.EmergencyContactName,
            EmergencyContactPhone = m.EmergencyContactPhone,
            Gender = m.Gender,
            Address = m.Address,
            MedicalConditions = m.MedicalConditions,
            JoinDate = m.JoinDate,
            CurrentMembership = currentMembership,
            ActiveGoalsCount = m.Goals.Count(g => g.Status == GoalStatus.InProgress),
            TotalBookingsCount = m.Bookings.Count
        };
    }

    // ==========================================
    // MEMBERSHIPS & SUBSCRIPTIONS
    // ==========================================

    public async Task<PagedResult<MembershipDto>> GetMembershipsAsync(
        Guid? memberId,
        MembershipStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Run expiry detection update before query
        await DetectAndMarkExpiredMembershipsAsync(cancellationToken);

        var query = _context.Memberships
            .AsNoTracking()
            .Include(ms => ms.Member).ThenInclude(m => m.User)
            .Include(ms => ms.Plan)
            .AsQueryable();

        if (memberId.HasValue)
            query = query.Where(ms => ms.MemberId == memberId.Value);

        if (status.HasValue)
            query = query.Where(ms => ms.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(ms => ms.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ms => new MembershipDto
            {
                Id = ms.Id,
                MemberId = ms.MemberId,
                MemberName = $"{ms.Member.User.FirstName} {ms.Member.User.LastName}",
                MemberEmail = ms.Member.User.Email,
                PlanId = ms.PlanId,
                PlanName = ms.Plan.Name,
                StartDate = ms.StartDate,
                EndDate = ms.EndDate,
                Status = ms.Status,
                AutoRenew = ms.AutoRenew,
                PricePaid = ms.PricePaid,
                CreatedAt = ms.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<MembershipDto>.Create(items, total, page, pageSize);
    }

    public async Task<MembershipDto?> GetMyCurrentMembershipAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var member = await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (member == null) return null;

        await DetectAndMarkExpiredMembershipsAsync(cancellationToken);

        var current = await _context.Memberships
            .AsNoTracking()
            .Include(ms => ms.Member).ThenInclude(m => m.User)
            .Include(ms => ms.Plan)
            .Where(ms => ms.MemberId == member.Id && ms.Status == MembershipStatus.Active && ms.EndDate >= DateTime.UtcNow)
            .OrderByDescending(ms => ms.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (current == null)
        {
            // If no active, return latest historical record
            current = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Member).ThenInclude(m => m.User)
                .Include(ms => ms.Plan)
                .Where(ms => ms.MemberId == member.Id)
                .OrderByDescending(ms => ms.EndDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (current == null) return null;

        return new MembershipDto
        {
            Id = current.Id,
            MemberId = current.MemberId,
            MemberName = $"{current.Member.User.FirstName} {current.Member.User.LastName}",
            MemberEmail = current.Member.User.Email,
            PlanId = current.PlanId,
            PlanName = current.Plan.Name,
            StartDate = current.StartDate,
            EndDate = current.EndDate,
            Status = current.Status,
            AutoRenew = current.AutoRenew,
            PricePaid = current.PricePaid,
            CreatedAt = current.CreatedAt
        };
    }

    public async Task<List<MembershipDto>> GetMemberMembershipHistoryAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AsNoTracking()
            .Include(ms => ms.Member).ThenInclude(m => m.User)
            .Include(ms => ms.Plan)
            .Where(ms => ms.MemberId == memberId)
            .OrderByDescending(ms => ms.StartDate)
            .Select(ms => new MembershipDto
            {
                Id = ms.Id,
                MemberId = ms.MemberId,
                MemberName = $"{ms.Member.User.FirstName} {ms.Member.User.LastName}",
                MemberEmail = ms.Member.User.Email,
                PlanId = ms.PlanId,
                PlanName = ms.Plan.Name,
                StartDate = ms.StartDate,
                EndDate = ms.EndDate,
                Status = ms.Status,
                AutoRenew = ms.AutoRenew,
                PricePaid = ms.PricePaid,
                CreatedAt = ms.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MembershipDto> CreateMembershipAsync(CreateMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with ID '{request.MemberId}' was not found.");

        var plan = await _context.MembershipPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"Membership plan with ID '{request.PlanId}' was not found.");

        if (!plan.IsActive)
            throw new InvalidOperationException($"Cannot enroll in deactivated plan '{plan.Name}'.");

        var startDate = request.StartDate?.ToUniversalTime() ?? DateTime.UtcNow;
        var endDate = startDate.AddDays(plan.DurationDays);

        var membership = new Membership
        {
            MemberId = member.Id,
            PlanId = plan.Id,
            StartDate = startDate,
            EndDate = endDate,
            Status = MembershipStatus.Active,
            AutoRenew = request.AutoRenew,
            PricePaid = plan.Price,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Memberships.AddAsync(membership, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new MembershipDto
        {
            Id = membership.Id,
            MemberId = member.Id,
            MemberName = $"{member.User.FirstName} {member.User.LastName}",
            MemberEmail = member.User.Email,
            PlanId = plan.Id,
            PlanName = plan.Name,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status,
            AutoRenew = membership.AutoRenew,
            PricePaid = membership.PricePaid,
            CreatedAt = membership.CreatedAt
        };
    }

    public async Task<MembershipDto> RenewMembershipAsync(
        RenewMembershipRequest request,
        Guid currentUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        Guid memberId;
        if (isStaff && request.MemberId.HasValue)
        {
            memberId = request.MemberId.Value;
        }
        else
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUserId, cancellationToken)
                ?? throw new KeyNotFoundException("Member profile not found for authenticated user.");
            memberId = member.Id;
        }

        var memberEntity = await _context.Members
            .Include(m => m.User)
            .Include(m => m.Memberships)
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with ID '{memberId}' was not found.");

        var plan = await _context.MembershipPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"Membership plan with ID '{request.PlanId}' was not found.");

        if (!plan.IsActive)
            throw new InvalidOperationException($"Cannot renew with deactivated plan '{plan.Name}'.");

        // Business Rule: renewal extends validity correctly
        // Find latest active membership
        var latestActive = memberEntity.Memberships
            .Where(m => m.Status == MembershipStatus.Active && m.EndDate >= DateTime.UtcNow)
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefault();

        DateTime newStartDate;
        if (latestActive != null)
        {
            // Extension from existing end date
            newStartDate = latestActive.EndDate;
        }
        else
        {
            // Start fresh from today
            newStartDate = DateTime.UtcNow;
        }

        var newEndDate = newStartDate.AddDays(plan.DurationDays);

        var newMembership = new Membership
        {
            MemberId = memberEntity.Id,
            PlanId = plan.Id,
            StartDate = newStartDate,
            EndDate = newEndDate,
            Status = MembershipStatus.Active,
            AutoRenew = request.AutoRenew,
            PricePaid = plan.Price,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Memberships.AddAsync(newMembership, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Renewed membership for member '{MemberId}' with plan '{PlanName}'. Validity: {StartDate:d} to {EndDate:d}.",
            memberId, plan.Name, newStartDate, newEndDate);

        return new MembershipDto
        {
            Id = newMembership.Id,
            MemberId = memberEntity.Id,
            MemberName = $"{memberEntity.User.FirstName} {memberEntity.User.LastName}",
            MemberEmail = memberEntity.User.Email,
            PlanId = plan.Id,
            PlanName = plan.Name,
            StartDate = newMembership.StartDate,
            EndDate = newMembership.EndDate,
            Status = newMembership.Status,
            AutoRenew = newMembership.AutoRenew,
            PricePaid = newMembership.PricePaid,
            CreatedAt = newMembership.CreatedAt
        };
    }

    public async Task<MembershipDto> CancelMembershipAsync(
        Guid membershipId,
        CancelMembershipRequest request,
        Guid currentUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var membership = await _context.Memberships
            .Include(ms => ms.Member).ThenInclude(m => m.User)
            .Include(ms => ms.Plan)
            .FirstOrDefaultAsync(ms => ms.Id == membershipId, cancellationToken)
            ?? throw new KeyNotFoundException($"Membership with ID '{membershipId}' was not found.");

        if (!isStaff && membership.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to cancel this membership.");

        if (membership.Status == MembershipStatus.Cancelled)
            throw new InvalidOperationException("Membership is already cancelled.");

        membership.Status = MembershipStatus.Cancelled;
        membership.AutoRenew = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Membership '{MembershipId}' cancelled. Reason: '{Reason}'",
            membershipId, request.Reason);

        return new MembershipDto
        {
            Id = membership.Id,
            MemberId = membership.MemberId,
            MemberName = $"{membership.Member.User.FirstName} {membership.Member.User.LastName}",
            MemberEmail = membership.Member.User.Email,
            PlanId = membership.PlanId,
            PlanName = membership.Plan.Name,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status,
            AutoRenew = membership.AutoRenew,
            PricePaid = membership.PricePaid,
            CreatedAt = membership.CreatedAt
        };
    }

    private async Task DetectAndMarkExpiredMembershipsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await _context.Memberships
            .Where(m => m.Status == MembershipStatus.Active && m.EndDate < now)
            .ToListAsync(cancellationToken);

        if (expired.Any())
        {
            foreach (var sub in expired)
            {
                sub.Status = MembershipStatus.Expired;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // ==========================================
    // GOALS & PROGRESS
    // ==========================================

    public async Task<PagedResult<GoalDto>> GetGoalsAsync(
        Guid? memberId,
        GoalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Goals
            .AsNoTracking()
            .Include(g => g.Member).ThenInclude(m => m.User)
            .Include(g => g.ProgressRecords)
            .AsQueryable();

        if (memberId.HasValue)
            query = query.Where(g => g.MemberId == memberId.Value);

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GoalDto
            {
                Id = g.Id,
                MemberId = g.MemberId,
                MemberName = $"{g.Member.User.FirstName} {g.Member.User.LastName}",
                Title = g.Title,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue,
                Unit = g.Unit,
                TargetDate = g.TargetDate,
                Status = g.Status,
                TotalLogsCount = g.ProgressRecords.Count,
                LastLogDate = g.ProgressRecords.OrderByDescending(p => p.RecordedDate).Select(p => (DateTime?)p.RecordedDate).FirstOrDefault(),
                CreatedAt = g.CreatedAt,
                RecentLogs = g.ProgressRecords.OrderByDescending(p => p.RecordedDate).Take(5).Select(p => new ProgressRecordDto
                {
                    Id = p.Id,
                    GoalId = p.GoalId,
                    GoalTitle = g.Title,
                    RecordedDate = p.RecordedDate,
                    Value = p.Value,
                    Notes = p.Notes,
                    RecordedByTrainerId = p.RecordedByTrainerId,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return PagedResult<GoalDto>.Create(items, total, page, pageSize);
    }

    public async Task<List<GoalDto>> GetMyGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var member = await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (member == null) return new List<GoalDto>();

        return await _context.Goals
            .AsNoTracking()
            .Include(g => g.Member).ThenInclude(m => m.User)
            .Include(g => g.ProgressRecords)
            .Where(g => g.MemberId == member.Id)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new GoalDto
            {
                Id = g.Id,
                MemberId = g.MemberId,
                MemberName = $"{g.Member.User.FirstName} {g.Member.User.LastName}",
                Title = g.Title,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue,
                Unit = g.Unit,
                TargetDate = g.TargetDate,
                Status = g.Status,
                TotalLogsCount = g.ProgressRecords.Count,
                LastLogDate = g.ProgressRecords.OrderByDescending(p => p.RecordedDate).Select(p => (DateTime?)p.RecordedDate).FirstOrDefault(),
                CreatedAt = g.CreatedAt,
                RecentLogs = g.ProgressRecords.OrderByDescending(p => p.RecordedDate).Take(5).Select(p => new ProgressRecordDto
                {
                    Id = p.Id,
                    GoalId = p.GoalId,
                    GoalTitle = g.Title,
                    RecordedDate = p.RecordedDate,
                    Value = p.Value,
                    Notes = p.Notes,
                    RecordedByTrainerId = p.RecordedByTrainerId,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GoalDto> GetGoalByIdAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var g = await _context.Goals
            .AsNoTracking()
            .Include(x => x.Member).ThenInclude(m => m.User)
            .Include(x => x.ProgressRecords)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness goal with ID '{id}' was not found.");

        if (!isStaff && g.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to view this goal.");

        return new GoalDto
        {
            Id = g.Id,
            MemberId = g.MemberId,
            MemberName = $"{g.Member.User.FirstName} {g.Member.User.LastName}",
            Title = g.Title,
            TargetValue = g.TargetValue,
            CurrentValue = g.CurrentValue,
            Unit = g.Unit,
            TargetDate = g.TargetDate,
            Status = g.Status,
            TotalLogsCount = g.ProgressRecords.Count,
            LastLogDate = g.ProgressRecords.OrderByDescending(p => p.RecordedDate).Select(p => (DateTime?)p.RecordedDate).FirstOrDefault(),
            CreatedAt = g.CreatedAt,
            RecentLogs = g.ProgressRecords.OrderByDescending(p => p.RecordedDate).Select(p => new ProgressRecordDto
            {
                Id = p.Id,
                GoalId = p.GoalId,
                GoalTitle = g.Title,
                RecordedDate = p.RecordedDate,
                Value = p.Value,
                Notes = p.Notes,
                RecordedByTrainerId = p.RecordedByTrainerId,
                CreatedAt = p.CreatedAt
            }).ToList()
        };
    }

    public async Task<GoalDto> CreateGoalAsync(CreateGoalRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        Guid memberId;
        if (isStaff && request.MemberId.HasValue)
        {
            memberId = request.MemberId.Value;
        }
        else
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUserId, cancellationToken)
                ?? throw new KeyNotFoundException("Member profile not found for authenticated user.");
            memberId = member.Id;
        }

        var memberEntity = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with ID '{memberId}' was not found.");

        if (request.TargetValue <= 0)
            throw new ArgumentException("Target value must be strictly positive.");

        if (request.TargetDate <= DateTime.UtcNow.AddMinutes(-5))
            throw new ArgumentException("Goal Target Date cannot be set in the past.");

        var goal = new Goal
        {
            MemberId = memberEntity.Id,
            Title = request.Title.Trim(),
            TargetValue = request.TargetValue,
            CurrentValue = request.CurrentValue,
            Unit = request.Unit.Trim(),
            TargetDate = request.TargetDate.ToUniversalTime(),
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Goals.AddAsync(goal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // If initial value logged, create initial progress record
        if (request.CurrentValue > 0)
        {
            var initialRecord = new ProgressRecord
            {
                GoalId = goal.Id,
                RecordedDate = DateTime.UtcNow,
                Value = request.CurrentValue,
                Notes = "Initial baseline measurement",
                CreatedAt = DateTime.UtcNow
            };
            await _context.ProgressRecords.AddAsync(initialRecord, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new GoalDto
        {
            Id = goal.Id,
            MemberId = memberEntity.Id,
            MemberName = $"{memberEntity.User.FirstName} {memberEntity.User.LastName}",
            Title = goal.Title,
            TargetValue = goal.TargetValue,
            CurrentValue = goal.CurrentValue,
            Unit = goal.Unit,
            TargetDate = goal.TargetDate,
            Status = goal.Status,
            TotalLogsCount = request.CurrentValue > 0 ? 1 : 0,
            CreatedAt = goal.CreatedAt
        };
    }

    public async Task<GoalDto> UpdateGoalAsync(Guid id, UpdateGoalRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var goal = await _context.Goals
            .Include(g => g.Member).ThenInclude(m => m.User)
            .Include(g => g.ProgressRecords)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness goal with ID '{id}' was not found.");

        if (!isStaff && goal.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to update this goal.");

        if (request.TargetValue <= 0)
            throw new ArgumentException("Target value must be strictly positive.");

        goal.Title = request.Title.Trim();
        goal.TargetValue = request.TargetValue;
        goal.Unit = request.Unit.Trim();
        goal.TargetDate = request.TargetDate.ToUniversalTime();
        goal.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return new GoalDto
        {
            Id = goal.Id,
            MemberId = goal.MemberId,
            MemberName = $"{goal.Member.User.FirstName} {goal.Member.User.LastName}",
            Title = goal.Title,
            TargetValue = goal.TargetValue,
            CurrentValue = goal.CurrentValue,
            Unit = goal.Unit,
            TargetDate = goal.TargetDate,
            Status = goal.Status,
            TotalLogsCount = goal.ProgressRecords.Count,
            CreatedAt = goal.CreatedAt
        };
    }

    public async Task<GoalDto> CompleteGoalAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var goal = await _context.Goals
            .Include(g => g.Member).ThenInclude(m => m.User)
            .Include(g => g.ProgressRecords)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness goal with ID '{id}' was not found.");

        if (!isStaff && goal.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to complete this goal.");

        goal.Status = GoalStatus.Achieved;
        await _context.SaveChangesAsync(cancellationToken);

        return new GoalDto
        {
            Id = goal.Id,
            MemberId = goal.MemberId,
            MemberName = $"{goal.Member.User.FirstName} {goal.Member.User.LastName}",
            Title = goal.Title,
            TargetValue = goal.TargetValue,
            CurrentValue = goal.CurrentValue,
            Unit = goal.Unit,
            TargetDate = goal.TargetDate,
            Status = goal.Status,
            TotalLogsCount = goal.ProgressRecords.Count,
            CreatedAt = goal.CreatedAt
        };
    }

    public async Task DeleteGoalAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var goal = await _context.Goals
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fitness goal with ID '{id}' was not found.");

        if (!isStaff && goal.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to delete this goal.");

        _context.Goals.Remove(goal);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // PROGRESS RECORDS
    // ==========================================

    public async Task<List<ProgressRecordDto>> GetGoalProgressHistoryAsync(
        Guid goalId,
        Guid currentUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var goal = await _context.Goals
            .AsNoTracking()
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == goalId, cancellationToken)
            ?? throw new KeyNotFoundException($"Goal with ID '{goalId}' was not found.");

        if (!isStaff && goal.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to view progress for this goal.");

        return await _context.ProgressRecords
            .AsNoTracking()
            .Where(p => p.GoalId == goalId)
            .OrderByDescending(p => p.RecordedDate)
            .Select(p => new ProgressRecordDto
            {
                Id = p.Id,
                GoalId = p.GoalId,
                GoalTitle = goal.Title,
                RecordedDate = p.RecordedDate,
                Value = p.Value,
                Notes = p.Notes,
                RecordedByTrainerId = p.RecordedByTrainerId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProgressRecordDto> RecordProgressAsync(
        CreateProgressRecordRequest request,
        Guid currentUserId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var goal = await _context.Goals
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken)
            ?? throw new KeyNotFoundException($"Goal with ID '{request.GoalId}' was not found.");

        if (!isStaff && goal.Member.UserId != currentUserId)
            throw new ForbiddenException("You are not authorized to record progress for this goal.");

        if (request.Value < 0)
            throw new ArgumentException("Measurement progress value cannot be negative.");

        var recordDate = request.RecordedDate?.ToUniversalTime() ?? DateTime.UtcNow;

        var record = new ProgressRecord
        {
            GoalId = goal.Id,
            RecordedDate = recordDate,
            Value = request.Value,
            Notes = request.Notes?.Trim(),
            RecordedByTrainerId = isStaff ? currentUserId : null,
            CreatedAt = DateTime.UtcNow
        };

        // Update goal's currentValue
        goal.CurrentValue = request.Value;

        // Auto-complete goal if target reached
        if (goal.Status == GoalStatus.InProgress && goal.CurrentValue >= goal.TargetValue)
        {
            goal.Status = GoalStatus.Achieved;
        }

        await _context.ProgressRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProgressRecordDto
        {
            Id = record.Id,
            GoalId = record.GoalId,
            GoalTitle = goal.Title,
            RecordedDate = record.RecordedDate,
            Value = record.Value,
            Notes = record.Notes,
            RecordedByTrainerId = record.RecordedByTrainerId,
            CreatedAt = record.CreatedAt
        };
    }

    // ==========================================
    // ANALYTICS
    // ==========================================

    public async Task<MembershipAnalyticsDto> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        await DetectAndMarkExpiredMembershipsAsync(cancellationToken);

        var totalMembers = await _context.Members.CountAsync(cancellationToken);
        var activeMemberships = await _context.Memberships.CountAsync(m => m.Status == MembershipStatus.Active, cancellationToken);
        var expiredMemberships = await _context.Memberships.CountAsync(m => m.Status == MembershipStatus.Expired, cancellationToken);
        var cancelledMemberships = await _context.Memberships.CountAsync(m => m.Status == MembershipStatus.Cancelled, cancellationToken);
        var totalRevenue = await _context.Memberships.SumAsync(m => m.PricePaid, cancellationToken);

        var totalGoals = await _context.Goals.CountAsync(cancellationToken);
        var achievedGoals = await _context.Goals.CountAsync(g => g.Status == GoalStatus.Achieved, cancellationToken);

        var distribution = await _context.MembershipPlans
            .AsNoTracking()
            .Select(p => new PlanDistributionItem
            {
                PlanName = p.Name,
                Count = p.Memberships.Count(m => m.Status == MembershipStatus.Active),
                Revenue = p.Memberships.Sum(m => m.PricePaid)
            })
            .ToListAsync(cancellationToken);

        return new MembershipAnalyticsDto
        {
            TotalMembers = totalMembers,
            ActiveMemberships = activeMemberships,
            ExpiredMemberships = expiredMemberships,
            CancelledMemberships = cancelledMemberships,
            TotalRevenue = totalRevenue,
            TotalGoals = totalGoals,
            AchievedGoals = achievedGoals,
            PlanDistribution = distribution
        };
    }
}
