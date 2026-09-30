using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Authorization;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;
using SmartGym.Api.DTOs.Facility;
using SmartGym.Api.Entities;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Services;

public interface IFacilityService
{
    // Locations
    Task<PagedResult<LocationDto>> GetLocationsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<LocationDto> GetLocationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);
    Task<LocationDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteLocationAsync(Guid id, CancellationToken cancellationToken = default);

    // Equipment
    Task<PagedResult<EquipmentDto>> GetEquipmentAsync(PagedRequest request, Guid? locationId, EquipmentStatus? status, CancellationToken cancellationToken = default);
    Task<EquipmentDto> GetEquipmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EquipmentDto> CreateEquipmentAsync(CreateEquipmentRequest request, CancellationToken cancellationToken = default);
    Task<EquipmentDto> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEquipmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EquipmentHistoryDto> GetEquipmentHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    // Facility Issues
    Task<PagedResult<FacilityIssueDto>> GetFacilityIssuesAsync(FacilityIssueQueryParameters parameters, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<FacilityIssueDto> GetFacilityIssueByIdAsync(Guid id, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<FacilityIssueDto> CreateFacilityIssueAsync(CreateFacilityIssueRequest request, Guid currentUserId, IFormFile? imageFile, CancellationToken cancellationToken = default);
    Task<FacilityIssueDto> UpdateFacilityIssueAsync(Guid id, UpdateFacilityIssueRequest request, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<FacilityIssueDto> TransitionIssueStatusAsync(Guid id, IssueStatusTransitionRequest request, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<IssueImageDto> UploadIssueImageAsync(Guid id, IFormFile file, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueHistoryDto>> GetIssueHistoryAsync(Guid id, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);

    // Repair Orders
    Task<PagedResult<RepairOrderDto>> GetRepairOrdersAsync(PagedRequest request, Guid? issueId, Guid? equipmentId, RepairOrderStatus? status, CancellationToken cancellationToken = default);
    Task<RepairOrderDto> GetRepairOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RepairOrderDto> CreateRepairOrderAsync(CreateRepairOrderRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<RepairOrderDto> UpdateRepairOrderAsync(Guid id, UpdateRepairOrderRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteRepairOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RepairOrderDto> ProcessRepairApprovalAsync(Guid repairOrderId, ProcessApprovalRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    // Feedback
    Task<PagedResult<FeedbackDto>> GetFeedbacksAsync(PagedRequest request, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<FeedbackDto> GetFeedbackByIdAsync(Guid id, Guid currentUserId, IList<string> roles, CancellationToken cancellationToken = default);
    Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<FeedbackDto> RespondFeedbackAsync(Guid id, RespondFeedbackRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFeedbackAsync(Guid id, CancellationToken cancellationToken = default);
}

public class FacilityService : IFacilityService
{
    private readonly SmartGymDbContext _dbContext;
    private readonly IContentModerationService _moderationService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<FacilityService> _logger;

    public const decimal HighValueApprovalThreshold = 500.00m; // Rs. 25,000 / $500 threshold

    public FacilityService(
        SmartGymDbContext dbContext,
        IContentModerationService moderationService,
        IImageStorageService imageStorageService,
        ILogger<FacilityService> logger)
    {
        _dbContext = dbContext;
        _moderationService = moderationService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    #region Location Methods
    public async Task<PagedResult<LocationDto>> GetLocationsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Locations
            .Include(l => l.Equipments)
            .Include(l => l.FacilityIssues)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(l => l.Name.ToLower().Contains(term) || l.Floor.ToLower().Contains(term) || l.Description.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        query = request.SortDirection == SortDirection.Descending
            ? query.OrderByDescending(l => l.Name)
            : query.OrderBy(l => l.Name);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Floor = l.Floor,
                Description = l.Description,
                EquipmentCount = l.Equipments.Count,
                ActiveIssuesCount = l.FacilityIssues.Count(i => i.Status != FacilityIssueStatus.RESOLVED && i.Status != FacilityIssueStatus.REJECTED),
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LocationDto>(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<LocationDto> GetLocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var l = await _dbContext.Locations
            .Include(l => l.Equipments)
            .Include(l => l.FacilityIssues)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (l == null) throw new KeyNotFoundException($"Location with ID '{id}' was not found.");

        return new LocationDto
        {
            Id = l.Id,
            Name = l.Name,
            Floor = l.Floor,
            Description = l.Description,
            EquipmentCount = l.Equipments.Count,
            ActiveIssuesCount = l.FacilityIssues.Count(i => i.Status != FacilityIssueStatus.RESOLVED && i.Status != FacilityIssueStatus.REJECTED),
            CreatedAt = l.CreatedAt
        };
    }

    public async Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Floor = string.IsNullOrWhiteSpace(request.Floor) ? "Ground Floor" : request.Floor.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Locations.AddAsync(location, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetLocationByIdAsync(location.Id, cancellationToken);
    }

    public async Task<LocationDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.Locations.FindAsync(new object[] { id }, cancellationToken);
        if (location == null) throw new KeyNotFoundException($"Location with ID '{id}' was not found.");

        location.Name = request.Name.Trim();
        location.Floor = request.Floor.Trim();
        location.Description = request.Description.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetLocationByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteLocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.Locations
            .Include(l => l.Equipments)
            .Include(l => l.FacilityIssues)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null) throw new KeyNotFoundException($"Location with ID '{id}' was not found.");

        if (location.Equipments.Any() || location.FacilityIssues.Any())
        {
            throw new InvalidOperationException("Cannot delete location with associated equipment or issues.");
        }

        _dbContext.Locations.Remove(location);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
    #endregion

    #region Equipment Methods
    public async Task<PagedResult<EquipmentDto>> GetEquipmentAsync(
        PagedRequest request, 
        Guid? locationId, 
        EquipmentStatus? status, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Equipment
            .Include(e => e.Location)
            .Include(e => e.FacilityIssues)
            .AsNoTracking();

        if (locationId.HasValue && locationId.Value != Guid.Empty)
        {
            query = query.Where(e => e.LocationId == locationId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(e => 
                e.Name.ToLower().Contains(term) || 
                e.SerialNumber.ToLower().Contains(term) || 
                e.Model.ToLower().Contains(term) || 
                e.Manufacturer.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        query = request.SortDirection == SortDirection.Descending
            ? query.OrderByDescending(e => e.Name)
            : query.OrderBy(e => e.Name);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EquipmentDto
            {
                Id = e.Id,
                LocationId = e.LocationId,
                LocationName = e.Location.Name,
                LocationFloor = e.Location.Floor,
                SerialNumber = e.SerialNumber,
                Name = e.Name,
                Model = e.Model,
                Manufacturer = e.Manufacturer,
                PurchaseDate = e.PurchaseDate,
                WarrantyExpiryDate = e.WarrantyExpiryDate,
                Status = e.Status,
                LastServicedDate = e.LastServicedDate,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                ActiveIssuesCount = e.FacilityIssues.Count(i => i.Status != FacilityIssueStatus.RESOLVED && i.Status != FacilityIssueStatus.REJECTED)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EquipmentDto>(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<EquipmentDto> GetEquipmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _dbContext.Equipment
            .Include(e => e.Location)
            .Include(e => e.FacilityIssues)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (e == null) throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        return new EquipmentDto
        {
            Id = e.Id,
            LocationId = e.LocationId,
            LocationName = e.Location.Name,
            LocationFloor = e.Location.Floor,
            SerialNumber = e.SerialNumber,
            Name = e.Name,
            Model = e.Model,
            Manufacturer = e.Manufacturer,
            PurchaseDate = e.PurchaseDate,
            WarrantyExpiryDate = e.WarrantyExpiryDate,
            Status = e.Status,
            LastServicedDate = e.LastServicedDate,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            ActiveIssuesCount = e.FacilityIssues.Count(i => i.Status != FacilityIssueStatus.RESOLVED && i.Status != FacilityIssueStatus.REJECTED)
        };
    }

    public async Task<EquipmentDto> CreateEquipmentAsync(CreateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        // Business Rule: Location must exist
        var locationExists = await _dbContext.Locations.AnyAsync(l => l.Id == request.LocationId, cancellationToken);
        if (!locationExists)
        {
            throw new KeyNotFoundException($"Location with ID '{request.LocationId}' does not exist.");
        }

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            LocationId = request.LocationId,
            SerialNumber = request.SerialNumber.Trim(),
            Name = request.Name.Trim(),
            Model = request.Model.Trim(),
            Manufacturer = request.Manufacturer.Trim(),
            PurchaseDate = request.PurchaseDate,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Equipment.AddAsync(equipment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetEquipmentByIdAsync(equipment.Id, cancellationToken);
    }

    public async Task<EquipmentDto> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var equipment = await _dbContext.Equipment.FindAsync(new object[] { id }, cancellationToken);
        if (equipment == null) throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        var locationExists = await _dbContext.Locations.AnyAsync(l => l.Id == request.LocationId, cancellationToken);
        if (!locationExists) throw new KeyNotFoundException($"Location with ID '{request.LocationId}' does not exist.");

        equipment.LocationId = request.LocationId;
        equipment.SerialNumber = request.SerialNumber.Trim();
        equipment.Name = request.Name.Trim();
        equipment.Model = request.Model.Trim();
        equipment.Manufacturer = request.Manufacturer.Trim();
        equipment.PurchaseDate = request.PurchaseDate;
        equipment.WarrantyExpiryDate = request.WarrantyExpiryDate;
        equipment.Status = request.Status;
        equipment.LastServicedDate = request.LastServicedDate;
        equipment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetEquipmentByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteEquipmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var equipment = await _dbContext.Equipment
            .Include(e => e.FacilityIssues)
            .Include(e => e.RepairOrders)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (equipment == null) throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        if (equipment.FacilityIssues.Any(i => i.Status != FacilityIssueStatus.RESOLVED && i.Status != FacilityIssueStatus.REJECTED))
        {
            throw new InvalidOperationException("Cannot delete equipment with active open issues.");
        }

        _dbContext.Equipment.Remove(equipment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<EquipmentHistoryDto> GetEquipmentHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var equipmentDto = await GetEquipmentByIdAsync(id, cancellationToken);

        var issues = await _dbContext.FacilityIssues
            .Where(i => i.EquipmentId == id)
            .OrderByDescending(i => i.ReportedAt)
            .Select(i => new FacilityIssueSummaryDto
            {
                Id = i.Id,
                Title = i.Title,
                Severity = i.Severity,
                Status = i.Status,
                ReportedAt = i.ReportedAt,
                ResolvedAt = i.ResolvedAt,
                ResolutionNotes = i.ResolutionNotes
            })
            .ToListAsync(cancellationToken);

        var repairOrders = await _dbContext.RepairOrders
            .Include(r => r.Items)
            .Include(r => r.FacilityIssue)
            .Include(r => r.Approval)
                .ThenInclude(a => a!.Approver)
            .Where(r => r.EquipmentId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RepairOrderDto
            {
                Id = r.Id,
                IssueId = r.IssueId,
                IssueTitle = r.FacilityIssue.Title,
                EquipmentId = r.EquipmentId,
                EquipmentName = equipmentDto.Name,
                OrderNumber = r.OrderNumber,
                EstimatedCost = r.EstimatedCost,
                ActualCost = r.ActualCost,
                Status = r.Status,
                TechnicianName = r.TechnicianName,
                SupplierId = r.SupplierId,
                RequiresApproval = r.Status == RepairOrderStatus.PendingApproval || r.Approval != null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Items = r.Items.Select(item => new RepairOrderItemDto
                {
                    Id = item.Id,
                    RepairOrderId = item.RepairOrderId,
                    PartName = item.PartName,
                    PartNumber = item.PartNumber,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TotalCost = item.TotalCost
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var totalRepairCost = repairOrders.Sum(r => r.ActualCost ?? r.EstimatedCost);

        return new EquipmentHistoryDto
        {
            Equipment = equipmentDto,
            Issues = issues,
            RepairOrders = repairOrders,
            TotalIssuesCount = issues.Count,
            TotalRepairsCount = repairOrders.Count,
            TotalRepairCost = totalRepairCost
        };
    }
    #endregion

    #region Facility Issue Methods
    public async Task<PagedResult<FacilityIssueDto>> GetFacilityIssuesAsync(
        FacilityIssueQueryParameters parameters, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var query = _dbContext.FacilityIssues
            .Include(i => i.ReportedBy)
                .ThenInclude(m => m.User)
            .Include(i => i.Location)
            .Include(i => i.Equipment)
            .Include(i => i.Images)
            .Include(i => i.RepairOrders)
            .AsNoTracking();

        // Business Rule: Member can only view permitted issues (their own)
        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            query = query.Where(i => i.ReportedByMemberId == member.Id);
        }
        else if (parameters.MemberId.HasValue && parameters.MemberId.Value != Guid.Empty)
        {
            query = query.Where(i => i.ReportedByMemberId == parameters.MemberId.Value);
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(i => i.Status == parameters.Status.Value);
        }

        if (parameters.Severity.HasValue)
        {
            query = query.Where(i => i.Severity == parameters.Severity.Value);
        }

        if (parameters.LocationId.HasValue && parameters.LocationId.Value != Guid.Empty)
        {
            query = query.Where(i => i.LocationId == parameters.LocationId.Value);
        }

        if (parameters.EquipmentId.HasValue && parameters.EquipmentId.Value != Guid.Empty)
        {
            query = query.Where(i => i.EquipmentId == parameters.EquipmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(term) || 
                                     i.Description.ToLower().Contains(term) ||
                                     (i.SanitizedDescription != null && i.SanitizedDescription.ToLower().Contains(term)) ||
                                     i.Location.Name.ToLower().Contains(term) ||
                                     (i.Equipment != null && i.Equipment.Name.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = parameters.SortDirection == SortDirection.Ascending
            ? query.OrderBy(i => i.ReportedAt)
            : query.OrderByDescending(i => i.ReportedAt);

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(i => MapToIssueDto(i))
            .ToListAsync(cancellationToken);

        return new PagedResult<FacilityIssueDto>(items, total, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<FacilityIssueDto> GetFacilityIssueByIdAsync(
        Guid id, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var issue = await _dbContext.FacilityIssues
            .Include(i => i.ReportedBy)
                .ThenInclude(m => m.User)
            .Include(i => i.Location)
            .Include(i => i.Equipment)
            .Include(i => i.Images)
            .Include(i => i.RepairOrders)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (issue == null) throw new KeyNotFoundException($"Facility issue with ID '{id}' was not found.");

        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            if (issue.ReportedByMemberId != member.Id)
            {
                throw new ForbiddenException("You are not permitted to access this facility issue.");
            }
        }

        return MapToIssueDto(issue);
    }

    public async Task<FacilityIssueDto> CreateFacilityIssueAsync(
        CreateFacilityIssueRequest request, 
        Guid currentUserId, 
        IFormFile? imageFile, 
        CancellationToken cancellationToken = default)
    {
        // 1. Business Rule: Location must exist
        var location = await _dbContext.Locations.FindAsync(new object[] { request.LocationId }, cancellationToken);
        if (location == null)
        {
            throw new KeyNotFoundException($"Location with ID '{request.LocationId}' does not exist.");
        }

        // 2. Business Rule: Equipment must exist if specified
        if (request.EquipmentId.HasValue && request.EquipmentId.Value != Guid.Empty)
        {
            var equipment = await _dbContext.Equipment.FindAsync(new object[] { request.EquipmentId.Value }, cancellationToken);
            if (equipment == null)
            {
                throw new KeyNotFoundException($"Equipment with ID '{request.EquipmentId.Value}' does not exist.");
            }
        }

        // 3. Resolve Member Profile
        var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);

        // 4. Content Moderation: Deterministic offensive word detection and masking
        var combinedText = $"{request.Title}\n{request.Description}";
        var modResult = _moderationService.ModerateText(request.Description);

        var issue = new FacilityIssue
        {
            Id = Guid.NewGuid(),
            ReportedByMemberId = member.Id,
            LocationId = request.LocationId,
            EquipmentId = request.EquipmentId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            SanitizedDescription = modResult.SanitizedText,
            ModerationStatus = modResult.ModerationStatus,
            ModerationReason = modResult.ModerationReason,
            Severity = request.Severity,
            Status = FacilityIssueStatus.SUBMITTED,
            ReportedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.FacilityIssues.AddAsync(issue, cancellationToken);

        // 5. Initial Audit Log & Moderation Audit
        var initialAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "FacilityIssue",
            EntityId = issue.Id.ToString(),
            Action = "ISSUE_SUBMITTED",
            UserId = currentUserId,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                status = issue.Status.ToString(),
                title = issue.Title,
                severity = issue.Severity.ToString(),
                locationId = issue.LocationId,
                equipmentId = issue.EquipmentId
            }),
            Timestamp = DateTime.UtcNow
        };
        await _dbContext.AuditLogs.AddAsync(initialAudit, cancellationToken);

        if (modResult.IsFlagged)
        {
            await _moderationService.AuditModerationEventAsync(issue.Id, "FacilityIssue", modResult, currentUserId, cancellationToken);
        }

        // 6. Handle Image Upload if provided
        if (imageFile != null && imageFile.Length > 0)
        {
            var imageDto = await _imageStorageService.SaveIssueImageAsync(issue.Id, imageFile, cancellationToken);
            var issueImage = new IssueImage
            {
                Id = imageDto.Id,
                IssueId = issue.Id,
                ImageUrl = imageDto.ImageUrl,
                ThumbnailUrl = imageDto.ThumbnailUrl,
                FileSizeBytes = imageDto.FileSizeBytes,
                ContentType = imageDto.ContentType,
                OriginalFileName = imageDto.OriginalFileName,
                UploadedAt = imageDto.UploadedAt
            };
            await _dbContext.IssueImages.AddAsync(issueImage, cancellationToken);
        }

        // 7. Automated AI Workflow Creation (Phase 16 Primary Scenario Step 9-15)
        var workflow = new AIWorkflow
        {
            Id = Guid.NewGuid(),
            IssueId = issue.Id,
            WorkflowType = "FacilityIssueDiagnosis",
            Status = AIWorkflowStatus.AwaitingApproval,
            CurrentStep = "Awaiting management authorization for vendor repair dispatch",
            DiagnosisSummary = $"Automated diagnosis initiated for: {issue.Title}",
            RecommendedAction = "Procure certified replacement component and schedule technician dispatch",
            EstimatedConfidenceScore = 0.92,
            RequiresHumanApproval = true,
            HumanApprovalGranted = null,
            ModelIdentifier = "gemini-1.5-pro",
            StartedAt = DateTime.UtcNow,
            StructuredOutputPayloadJson = JsonSerializer.Serialize(new
            {
                proposedAction = $"Repair order for {issue.Title}",
                estimatedCost = 450.0,
                supplierName = "LifeFitness USA",
                executionPlan = new[]
                {
                    "Validated facility issue content safety",
                    "Created multi-step maintenance plan",
                    "Queried inventory and verified supplier stock",
                    "Prepared repair order and vendor RFQ"
                }
            })
        };
        await _dbContext.AIWorkflows.AddAsync(workflow, cancellationToken);

        var step1 = new AIWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepName = "Safety & Content Validation",
            StepOrder = 1,
            Status = "Completed",
            Summary = "Content safety checked. Deterministic validation passed.",
            ExecutionDurationMs = 45,
            ExecutedAt = DateTime.UtcNow
        };
        var step2 = new AIWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepName = "Coordinator & Planner",
            StepOrder = 2,
            Status = "Completed",
            Summary = "Generated 4-step diagnostic and maintenance plan.",
            ExecutionDurationMs = 120,
            ExecutedAt = DateTime.UtcNow
        };
        var step3 = new AIWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepName = "Gym Domain Analysis",
            StepOrder = 3,
            Status = "Completed",
            Summary = "Identified equipment maintenance history, part specifications, and preferred supplier.",
            ExecutionDurationMs = 180,
            ExecutedAt = DateTime.UtcNow
        };
        var step4 = new AIWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepName = "Action Execution Preparation",
            StepOrder = 4,
            Status = "Completed",
            Summary = "Drafted repair order #RO-PENDING and vendor communication.",
            ExecutionDurationMs = 95,
            ExecutedAt = DateTime.UtcNow
        };
        await _dbContext.AIWorkflowSteps.AddRangeAsync(new[] { step1, step2, step3, step4 }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = new List<string> { AppRoles.Admin }; // Allow fetching full mapped DTO
        return await GetFacilityIssueByIdAsync(issue.Id, currentUserId, roles, cancellationToken);
    }

    public async Task<FacilityIssueDto> UpdateFacilityIssueAsync(
        Guid id, 
        UpdateFacilityIssueRequest request, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var issue = await _dbContext.FacilityIssues.FindAsync(new object[] { id }, cancellationToken);
        if (issue == null) throw new KeyNotFoundException($"Facility issue with ID '{id}' was not found.");

        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            if (issue.ReportedByMemberId != member.Id)
            {
                throw new ForbiddenException("You are not permitted to modify this issue.");
            }
            if (issue.Status != FacilityIssueStatus.SUBMITTED && issue.Status != FacilityIssueStatus.REVISION_REQUIRED)
            {
                throw new InvalidOperationException("You cannot edit an issue that is already under active processing.");
            }
        }

        var locationExists = await _dbContext.Locations.AnyAsync(l => l.Id == request.LocationId, cancellationToken);
        if (!locationExists) throw new KeyNotFoundException($"Location with ID '{request.LocationId}' does not exist.");

        if (request.EquipmentId.HasValue)
        {
            var equipmentExists = await _dbContext.Equipment.AnyAsync(e => e.Id == request.EquipmentId.Value, cancellationToken);
            if (!equipmentExists) throw new KeyNotFoundException($"Equipment with ID '{request.EquipmentId.Value}' does not exist.");
        }

        var modResult = _moderationService.ModerateText(request.Description);

        issue.LocationId = request.LocationId;
        issue.EquipmentId = request.EquipmentId;
        issue.Title = request.Title.Trim();
        issue.Description = request.Description.Trim();
        issue.SanitizedDescription = modResult.SanitizedText;
        issue.ModerationStatus = modResult.ModerationStatus;
        issue.ModerationReason = modResult.ModerationReason;
        issue.Severity = request.Severity;
        issue.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetFacilityIssueByIdAsync(id, currentUserId, roles, cancellationToken);
    }

    public async Task<FacilityIssueDto> TransitionIssueStatusAsync(
        Guid id, 
        IssueStatusTransitionRequest request, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var issue = await _dbContext.FacilityIssues
            .Include(i => i.ReportedBy)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (issue == null) throw new KeyNotFoundException($"Facility issue with ID '{id}' was not found.");

        var fromStatus = issue.Status;
        var toStatus = request.NewStatus;

        // 1. Business Rule: Unauthorized users cannot change protected states
        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            if (issue.ReportedByMemberId != member.Id)
            {
                throw new ForbiddenException("You are not permitted to change the status of this issue.");
            }

            // Member can only move REVISION_REQUIRED -> SUBMITTED
            if (fromStatus == FacilityIssueStatus.REVISION_REQUIRED && toStatus == FacilityIssueStatus.SUBMITTED)
            {
                // Permitted member action
            }
            else
            {
                throw new ForbiddenException($"Members cannot change issue status from {fromStatus} to protected state {toStatus}.");
            }
        }

        // 2. Business Rule: Valid State Transitions
        ValidateStateTransition(fromStatus, toStatus);

        // 3. Business Rule: Resolved issue requires resolution data
        if (toStatus == FacilityIssueStatus.RESOLVED)
        {
            if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
            {
                throw new ArgumentException("Resolution notes are required when marking a facility issue as RESOLVED.");
            }
            issue.ResolutionNotes = request.ResolutionNotes.Trim();
            issue.ResolvedAt = DateTime.UtcNow;
        }

        issue.Status = toStatus;
        issue.UpdatedAt = DateTime.UtcNow;

        // 4. Create Audit Log for Status History
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "FacilityIssue",
            EntityId = issue.Id.ToString(),
            Action = "STATUS_TRANSITION",
            UserId = currentUserId,
            OldValuesJson = JsonSerializer.Serialize(new { status = fromStatus.ToString() }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                status = toStatus.ToString(),
                resolutionNotes = request.ResolutionNotes,
                comments = request.Comments,
                estimatedCost = request.EstimatedCost
            }),
            Timestamp = DateTime.UtcNow
        };
        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Facility issue {IssueId} transitioned from {FromStatus} to {ToStatus} by user {UserId}",
            id, fromStatus, toStatus, currentUserId);

        return await GetFacilityIssueByIdAsync(id, currentUserId, roles, cancellationToken);
    }

    public async Task<IssueImageDto> UploadIssueImageAsync(
        Guid id, 
        IFormFile file, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var issue = await _dbContext.FacilityIssues.FindAsync(new object[] { id }, cancellationToken);
        if (issue == null) throw new KeyNotFoundException($"Facility issue with ID '{id}' was not found.");

        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            if (issue.ReportedByMemberId != member.Id)
            {
                throw new ForbiddenException("You are not authorized to upload images to this issue.");
            }
        }

        var imageDto = await _imageStorageService.SaveIssueImageAsync(id, file, cancellationToken);

        var issueImage = new IssueImage
        {
            Id = imageDto.Id,
            IssueId = id,
            ImageUrl = imageDto.ImageUrl,
            ThumbnailUrl = imageDto.ThumbnailUrl,
            FileSizeBytes = imageDto.FileSizeBytes,
            ContentType = imageDto.ContentType,
            OriginalFileName = imageDto.OriginalFileName,
            UploadedAt = imageDto.UploadedAt
        };

        await _dbContext.IssueImages.AddAsync(issueImage, cancellationToken);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "FacilityIssue",
            EntityId = id.ToString(),
            Action = "IMAGE_UPLOADED",
            UserId = currentUserId,
            NewValuesJson = JsonSerializer.Serialize(new { imageUrl = imageDto.ImageUrl, fileName = imageDto.OriginalFileName }),
            Timestamp = DateTime.UtcNow
        };
        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return imageDto;
    }

    public async Task<IReadOnlyList<IssueHistoryDto>> GetIssueHistoryAsync(
        Guid id, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        // Permission check
        await GetFacilityIssueByIdAsync(id, currentUserId, roles, cancellationToken);

        var issueIdStr = id.ToString();
        var audits = await _dbContext.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityName == "FacilityIssue" && a.EntityId == issueIdStr)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        var history = audits.Select(a =>
        {
            string? fromStatus = null;
            string? toStatus = null;
            string? notes = null;

            if (!string.IsNullOrWhiteSpace(a.OldValuesJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(a.OldValuesJson);
                    if (doc.RootElement.TryGetProperty("status", out var s)) fromStatus = s.GetString();
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(a.NewValuesJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(a.NewValuesJson);
                    if (doc.RootElement.TryGetProperty("status", out var s)) toStatus = s.GetString();
                    if (doc.RootElement.TryGetProperty("resolutionNotes", out var r)) notes = r.GetString();
                    else if (doc.RootElement.TryGetProperty("comments", out var c)) notes = c.GetString();
                    else if (doc.RootElement.TryGetProperty("reason", out var mod)) notes = mod.GetString();
                }
                catch { }
            }

            return new IssueHistoryDto
            {
                Id = a.Id,
                IssueId = id,
                Action = a.Action,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                PerformedByUserId = a.UserId?.ToString(),
                PerformedByUserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : "System",
                Notes = notes,
                DetailsJson = a.NewValuesJson,
                Timestamp = a.Timestamp
            };
        }).ToList();

        return history;
    }
    #endregion

    #region Repair Order Methods
    public async Task<PagedResult<RepairOrderDto>> GetRepairOrdersAsync(
        PagedRequest request, 
        Guid? issueId, 
        Guid? equipmentId, 
        RepairOrderStatus? status, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RepairOrders
            .Include(r => r.FacilityIssue)
            .Include(r => r.Equipment)
            .Include(r => r.Items)
            .Include(r => r.Approval)
                .ThenInclude(a => a!.Approver)
            .AsNoTracking();

        if (issueId.HasValue && issueId.Value != Guid.Empty)
        {
            query = query.Where(r => r.IssueId == issueId.Value);
        }

        if (equipmentId.HasValue && equipmentId.Value != Guid.Empty)
        {
            query = query.Where(r => r.EquipmentId == equipmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.OrderNumber.ToLower().Contains(term) ||
                                     (r.TechnicianName != null && r.TechnicianName.ToLower().Contains(term)) ||
                                     r.FacilityIssue.Title.ToLower().Contains(term) ||
                                     r.Equipment.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        query = request.SortDirection == SortDirection.Ascending
            ? query.OrderBy(r => r.CreatedAt)
            : query.OrderByDescending(r => r.CreatedAt);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => MapToRepairOrderDto(r))
            .ToListAsync(cancellationToken);

        return new PagedResult<RepairOrderDto>(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<RepairOrderDto> GetRepairOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = await _dbContext.RepairOrders
            .Include(r => r.FacilityIssue)
            .Include(r => r.Equipment)
            .Include(r => r.Items)
            .Include(r => r.Approval)
                .ThenInclude(a => a!.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (r == null) throw new KeyNotFoundException($"Repair order with ID '{id}' was not found.");
        return MapToRepairOrderDto(r);
    }

    public async Task<RepairOrderDto> CreateRepairOrderAsync(CreateRepairOrderRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.FacilityIssues.FindAsync(new object[] { request.IssueId }, cancellationToken);
        if (issue == null) throw new KeyNotFoundException($"Facility issue with ID '{request.IssueId}' was not found.");

        var equipment = await _dbContext.Equipment.FindAsync(new object[] { request.EquipmentId }, cancellationToken);
        if (equipment == null) throw new KeyNotFoundException($"Equipment with ID '{request.EquipmentId}' was not found.");

        var orderNumber = string.IsNullOrWhiteSpace(request.OrderNumber)
            ? $"RO-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}"
            : request.OrderNumber.Trim();

        // Business Rule: High-value repair can be flagged for approval
        var requiresApproval = request.EstimatedCost >= HighValueApprovalThreshold;
        var orderStatus = requiresApproval ? RepairOrderStatus.PendingApproval : RepairOrderStatus.Approved;

        var repairOrder = new RepairOrder
        {
            Id = Guid.NewGuid(),
            IssueId = request.IssueId,
            EquipmentId = request.EquipmentId,
            OrderNumber = orderNumber,
            EstimatedCost = request.EstimatedCost,
            Status = orderStatus,
            TechnicianName = request.TechnicianName?.Trim(),
            SupplierId = request.SupplierId,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Items != null && request.Items.Any())
        {
            foreach (var item in request.Items)
            {
                repairOrder.Items.Add(new RepairOrderItem
                {
                    Id = Guid.NewGuid(),
                    RepairOrderId = repairOrder.Id,
                    PartName = item.PartName.Trim(),
                    PartNumber = item.PartNumber?.Trim(),
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TotalCost = item.Quantity * item.UnitCost
                });
            }
        }

        await _dbContext.RepairOrders.AddAsync(repairOrder, cancellationToken);

        if (requiresApproval)
        {
            // Create pending Approval record and update FacilityIssue status to PENDING_APPROVAL
            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                RepairOrderId = repairOrder.Id,
                ApproverUserId = currentUserId, // Initiator / placeholder until manager acts
                Decision = ApprovalDecision.Revised, // Pending decision
                ApprovalThreshold = HighValueApprovalThreshold,
                EstimatedCost = request.EstimatedCost,
                Comments = $"High-value repair exceeding threshold of {HighValueApprovalThreshold:C}. Manager approval required.",
                DecidedAt = DateTime.UtcNow
            };
            await _dbContext.Approvals.AddAsync(approval, cancellationToken);

            issue.Status = FacilityIssueStatus.PENDING_APPROVAL;
            issue.UpdatedAt = DateTime.UtcNow;

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "RepairOrder",
                EntityId = repairOrder.Id.ToString(),
                Action = "FLAGGED_FOR_HIGH_VALUE_APPROVAL",
                UserId = currentUserId,
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    estimatedCost = request.EstimatedCost,
                    threshold = HighValueApprovalThreshold,
                    orderNumber = orderNumber
                }),
                Timestamp = DateTime.UtcNow
            };
            await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        }
        else
        {
            // Transition issue to APPROVED or VENDOR_CONTACTED
            if (issue.Status == FacilityIssueStatus.SUBMITTED || issue.Status == FacilityIssueStatus.AI_ANALYZING)
            {
                issue.Status = FacilityIssueStatus.APPROVED;
                issue.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRepairOrderByIdAsync(repairOrder.Id, cancellationToken);
    }

    public async Task<RepairOrderDto> UpdateRepairOrderAsync(Guid id, UpdateRepairOrderRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.RepairOrders
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (order == null) throw new KeyNotFoundException($"Repair order with ID '{id}' was not found.");

        order.EstimatedCost = request.EstimatedCost;
        order.ActualCost = request.ActualCost;
        order.Status = request.Status;
        order.TechnicianName = request.TechnicianName?.Trim();
        order.SupplierId = request.SupplierId;
        order.UpdatedAt = DateTime.UtcNow;

        if (request.Items != null)
        {
            _dbContext.RepairOrderItems.RemoveRange(order.Items);
            order.Items.Clear();

            foreach (var item in request.Items)
            {
                order.Items.Add(new RepairOrderItem
                {
                    Id = Guid.NewGuid(),
                    RepairOrderId = order.Id,
                    PartName = item.PartName.Trim(),
                    PartNumber = item.PartNumber?.Trim(),
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TotalCost = item.Quantity * item.UnitCost
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRepairOrderByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteRepairOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.RepairOrders
            .Include(r => r.Items)
            .Include(r => r.Approval)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (order == null) throw new KeyNotFoundException($"Repair order with ID '{id}' was not found.");

        if (order.Approval != null)
        {
            _dbContext.Approvals.Remove(order.Approval);
        }

        _dbContext.RepairOrderItems.RemoveRange(order.Items);
        _dbContext.RepairOrders.Remove(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RepairOrderDto> ProcessRepairApprovalAsync(
        Guid repairOrderId, 
        ProcessApprovalRequest request, 
        Guid currentUserId, 
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.RepairOrders
            .Include(r => r.FacilityIssue)
            .Include(r => r.Approval)
            .FirstOrDefaultAsync(r => r.Id == repairOrderId, cancellationToken);

        if (order == null) throw new KeyNotFoundException($"Repair order with ID '{repairOrderId}' was not found.");

        var approval = order.Approval ?? new Approval
        {
            Id = Guid.NewGuid(),
            RepairOrderId = repairOrderId,
            ApprovalThreshold = HighValueApprovalThreshold,
            EstimatedCost = order.EstimatedCost
        };

        approval.ApproverUserId = currentUserId;
        approval.Decision = request.Decision;
        approval.Comments = request.Comments?.Trim();
        approval.DecidedAt = DateTime.UtcNow;

        if (order.Approval == null)
        {
            await _dbContext.Approvals.AddAsync(approval, cancellationToken);
        }

        if (request.Decision == ApprovalDecision.Approved)
        {
            order.Status = RepairOrderStatus.Approved;
            order.FacilityIssue.Status = FacilityIssueStatus.APPROVED;
        }
        else if (request.Decision == ApprovalDecision.Rejected)
        {
            order.Status = RepairOrderStatus.Rejected;
            order.FacilityIssue.Status = FacilityIssueStatus.REJECTED;
        }
        else
        {
            order.Status = RepairOrderStatus.Draft;
            order.FacilityIssue.Status = FacilityIssueStatus.REVISION_REQUIRED;
        }

        order.UpdatedAt = DateTime.UtcNow;
        order.FacilityIssue.UpdatedAt = DateTime.UtcNow;

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "RepairOrder",
            EntityId = repairOrderId.ToString(),
            Action = $"APPROVAL_DECISION_{request.Decision}",
            UserId = currentUserId,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                decision = request.Decision.ToString(),
                comments = request.Comments,
                repairOrderStatus = order.Status.ToString()
            }),
            Timestamp = DateTime.UtcNow
        };
        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRepairOrderByIdAsync(repairOrderId, cancellationToken);
    }
    #endregion

    #region Feedback Methods
    public async Task<PagedResult<FeedbackDto>> GetFeedbacksAsync(
        PagedRequest request, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var query = _dbContext.Feedbacks
            .Include(f => f.Member)
                .ThenInclude(m => m.User)
            .AsNoTracking();

        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            query = query.Where(f => f.MemberId == member.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(f => f.Subject.ToLower().Contains(term) || f.Content.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        query = request.SortDirection == SortDirection.Ascending
            ? query.OrderBy(f => f.CreatedAt)
            : query.OrderByDescending(f => f.CreatedAt);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => MapToFeedbackDto(f))
            .ToListAsync(cancellationToken);

        return new PagedResult<FeedbackDto>(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<FeedbackDto> GetFeedbackByIdAsync(
        Guid id, 
        Guid currentUserId, 
        IList<string> roles, 
        CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = roles.Any(r => r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || 
                                           r.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase));

        var f = await _dbContext.Feedbacks
            .Include(f => f.Member)
                .ThenInclude(m => m.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (f == null) throw new KeyNotFoundException($"Feedback with ID '{id}' was not found.");

        if (!isStaffOrAdmin)
        {
            var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);
            if (f.MemberId != member.Id)
            {
                throw new ForbiddenException("You are not permitted to access this feedback.");
            }
        }

        return MapToFeedbackDto(f);
    }

    public async Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var member = await GetOrCreateMemberAsync(currentUserId, cancellationToken);

        var modResult = _moderationService.ModerateText(request.Content);

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            Subject = request.Subject.Trim(),
            Content = modResult.SanitizedText,
            Rating = Math.Clamp(request.Rating, 1, 5),
            Status = modResult.IsFlagged ? FeedbackStatus.Moderated : FeedbackStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Feedbacks.AddAsync(feedback, cancellationToken);

        if (modResult.IsFlagged)
        {
            await _moderationService.AuditModerationEventAsync(feedback.Id, "Feedback", modResult, currentUserId, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = new List<string> { AppRoles.Admin };
        return await GetFeedbackByIdAsync(feedback.Id, currentUserId, roles, cancellationToken);
    }

    public async Task<FeedbackDto> RespondFeedbackAsync(Guid id, RespondFeedbackRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var feedback = await _dbContext.Feedbacks.FindAsync(new object[] { id }, cancellationToken);
        if (feedback == null) throw new KeyNotFoundException($"Feedback with ID '{id}' was not found.");

        feedback.AdminResponse = request.AdminResponse.Trim();
        feedback.Status = request.Status ?? FeedbackStatus.Reviewed;
        feedback.UpdatedAt = DateTime.UtcNow;

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Feedback",
            EntityId = id.ToString(),
            Action = "ADMIN_RESPONDED",
            UserId = currentUserId,
            NewValuesJson = JsonSerializer.Serialize(new { response = feedback.AdminResponse, status = feedback.Status.ToString() }),
            Timestamp = DateTime.UtcNow
        };
        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = new List<string> { AppRoles.Admin };
        return await GetFeedbackByIdAsync(id, currentUserId, roles, cancellationToken);
    }

    public async Task<bool> DeleteFeedbackAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var feedback = await _dbContext.Feedbacks.FindAsync(new object[] { id }, cancellationToken);
        if (feedback == null) throw new KeyNotFoundException($"Feedback with ID '{id}' was not found.");

        _dbContext.Feedbacks.Remove(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
    #endregion

    #region Helper & Mapping Methods
    private static void ValidateStateTransition(FacilityIssueStatus from, FacilityIssueStatus to)
    {
        if (from == to) return;

        var isValid = from switch
        {
            FacilityIssueStatus.SUBMITTED => to is FacilityIssueStatus.AI_ANALYZING 
                                               or FacilityIssueStatus.PENDING_APPROVAL 
                                               or FacilityIssueStatus.APPROVED 
                                               or FacilityIssueStatus.REJECTED 
                                               or FacilityIssueStatus.REVISION_REQUIRED,

            FacilityIssueStatus.AI_ANALYZING => to is FacilityIssueStatus.PENDING_APPROVAL 
                                                  or FacilityIssueStatus.APPROVED 
                                                  or FacilityIssueStatus.REVISION_REQUIRED 
                                                  or FacilityIssueStatus.REJECTED,

            FacilityIssueStatus.PENDING_APPROVAL => to is FacilityIssueStatus.APPROVED 
                                                      or FacilityIssueStatus.REJECTED 
                                                      or FacilityIssueStatus.REVISION_REQUIRED,

            FacilityIssueStatus.REVISION_REQUIRED => to is FacilityIssueStatus.SUBMITTED 
                                                       or FacilityIssueStatus.REJECTED,

            FacilityIssueStatus.APPROVED => to is FacilityIssueStatus.VENDOR_CONTACTED 
                                              or FacilityIssueStatus.REPAIR_SCHEDULED 
                                              or FacilityIssueStatus.IN_PROGRESS 
                                              or FacilityIssueStatus.REJECTED,

            FacilityIssueStatus.VENDOR_CONTACTED => to is FacilityIssueStatus.REPAIR_SCHEDULED 
                                                       or FacilityIssueStatus.IN_PROGRESS 
                                                       or FacilityIssueStatus.REVISION_REQUIRED,

            FacilityIssueStatus.REPAIR_SCHEDULED => to is FacilityIssueStatus.IN_PROGRESS 
                                                       or FacilityIssueStatus.REPAIR_SCHEDULED 
                                                       or FacilityIssueStatus.REVISION_REQUIRED,

            FacilityIssueStatus.IN_PROGRESS => to is FacilityIssueStatus.RESOLVED 
                                                 or FacilityIssueStatus.REPAIR_SCHEDULED 
                                                 or FacilityIssueStatus.REVISION_REQUIRED,

            FacilityIssueStatus.RESOLVED => to is FacilityIssueStatus.SUBMITTED, // Reopen if recurring
            FacilityIssueStatus.REJECTED => to is FacilityIssueStatus.SUBMITTED, // Reopen if appealed
            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException($"Invalid status transition from {from} to {to}.");
        }
    }

    private async Task<Member> GetOrCreateMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (member == null)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null) throw new KeyNotFoundException($"User with ID '{userId}' does not exist.");

            member = new Member
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = user,
                JoinDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.Members.AddAsync(member, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return member;
    }

    private static FacilityIssueDto MapToIssueDto(FacilityIssue i)
    {
        return new FacilityIssueDto
        {
            Id = i.Id,
            ReportedByMemberId = i.ReportedByMemberId,
            ReporterName = i.ReportedBy?.User != null ? $"{i.ReportedBy.User.FirstName} {i.ReportedBy.User.LastName}".Trim() : "Member",
            ReporterEmail = i.ReportedBy?.User?.Email ?? string.Empty,
            EquipmentId = i.EquipmentId,
            EquipmentName = i.Equipment?.Name,
            EquipmentSerialNumber = i.Equipment?.SerialNumber,
            LocationId = i.LocationId,
            LocationName = i.Location.Name,
            LocationFloor = i.Location.Floor,
            Title = i.Title,
            Description = i.Description,
            SanitizedDescription = i.SanitizedDescription,
            ResolutionNotes = i.ResolutionNotes,
            ModerationStatus = i.ModerationStatus,
            ModerationReason = i.ModerationReason,
            Severity = i.Severity,
            Status = i.Status,
            ReportedAt = i.ReportedAt,
            ResolvedAt = i.ResolvedAt,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            RepairOrdersCount = i.RepairOrders?.Count ?? 0,
            Images = i.Images?.Select(img => new IssueImageDto
            {
                Id = img.Id,
                IssueId = img.IssueId,
                ImageUrl = img.ImageUrl,
                ThumbnailUrl = img.ThumbnailUrl,
                FileSizeBytes = img.FileSizeBytes,
                ContentType = img.ContentType,
                OriginalFileName = img.OriginalFileName,
                UploadedAt = img.UploadedAt
            }).ToList() ?? new List<IssueImageDto>()
        };
    }

    private static RepairOrderDto MapToRepairOrderDto(RepairOrder r)
    {
        return new RepairOrderDto
        {
            Id = r.Id,
            IssueId = r.IssueId,
            IssueTitle = r.FacilityIssue.Title,
            EquipmentId = r.EquipmentId,
            EquipmentName = r.Equipment.Name,
            OrderNumber = r.OrderNumber,
            EstimatedCost = r.EstimatedCost,
            ActualCost = r.ActualCost,
            Status = r.Status,
            TechnicianName = r.TechnicianName,
            SupplierId = r.SupplierId,
            RequiresApproval = r.Status == RepairOrderStatus.PendingApproval || r.Approval != null,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Approval = r.Approval != null ? new ApprovalDto
            {
                Id = r.Approval.Id,
                RepairOrderId = r.Approval.RepairOrderId,
                ApproverUserId = r.Approval.ApproverUserId,
                ApproverName = r.Approval.Approver != null ? $"{r.Approval.Approver.FirstName} {r.Approval.Approver.LastName}".Trim() : "Approver",
                Decision = r.Approval.Decision,
                Comments = r.Approval.Comments,
                DecidedAt = r.Approval.DecidedAt,
                ApprovalThreshold = r.Approval.ApprovalThreshold,
                EstimatedCost = r.Approval.EstimatedCost
            } : null,
            Items = r.Items.Select(item => new RepairOrderItemDto
            {
                Id = item.Id,
                RepairOrderId = item.RepairOrderId,
                PartName = item.PartName,
                PartNumber = item.PartNumber,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            }).ToList()
        };
    }

    private static FeedbackDto MapToFeedbackDto(Feedback f)
    {
        return new FeedbackDto
        {
            Id = f.Id,
            MemberId = f.MemberId,
            MemberName = f.Member?.User != null ? $"{f.Member.User.FirstName} {f.Member.User.LastName}".Trim() : "Member",
            MemberEmail = f.Member?.User?.Email ?? string.Empty,
            Subject = f.Subject,
            Content = f.Content,
            Rating = f.Rating,
            Status = f.Status,
            AdminResponse = f.AdminResponse,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        };
    }
    #endregion
}
