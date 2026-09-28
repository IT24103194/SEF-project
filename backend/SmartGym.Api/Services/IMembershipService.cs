using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Membership;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface IMembershipService
{
    // Membership Plans
    Task<List<MembershipPlanDto>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<MembershipPlanDto> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MembershipPlanDto> CreatePlanAsync(CreateMembershipPlanRequest request, CancellationToken cancellationToken = default);
    Task<MembershipPlanDto> UpdatePlanAsync(Guid id, UpdateMembershipPlanRequest request, CancellationToken cancellationToken = default);
    Task DeletePlanAsync(Guid id, CancellationToken cancellationToken = default);

    // Members
    Task<PagedResult<MemberDto>> GetMembersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MemberDto> GetMemberByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MemberDto> GetMemberByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MemberDto> UpdateMemberProfileAsync(Guid memberId, UpdateMemberProfileRequest request, CancellationToken cancellationToken = default);

    // Memberships
    Task<PagedResult<MembershipDto>> GetMembershipsAsync(Guid? memberId, MembershipStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MembershipDto?> GetMyCurrentMembershipAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<MembershipDto>> GetMemberMembershipHistoryAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<MembershipDto> CreateMembershipAsync(CreateMembershipRequest request, CancellationToken cancellationToken = default);
    Task<MembershipDto> RenewMembershipAsync(RenewMembershipRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<MembershipDto> CancelMembershipAsync(Guid membershipId, CancelMembershipRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);

    // Goals
    Task<PagedResult<GoalDto>> GetGoalsAsync(Guid? memberId, GoalStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<GoalDto>> GetMyGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GoalDto> GetGoalByIdAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<GoalDto> CreateGoalAsync(CreateGoalRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<GoalDto> UpdateGoalAsync(Guid id, UpdateGoalRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<GoalDto> CompleteGoalAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task DeleteGoalAsync(Guid id, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);

    // Progress Records
    Task<List<ProgressRecordDto>> GetGoalProgressHistoryAsync(Guid goalId, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);
    Task<ProgressRecordDto> RecordProgressAsync(CreateProgressRecordRequest request, Guid currentUserId, bool isStaff, CancellationToken cancellationToken = default);

    // Analytics
    Task<MembershipAnalyticsDto> GetAnalyticsAsync(CancellationToken cancellationToken = default);
}
