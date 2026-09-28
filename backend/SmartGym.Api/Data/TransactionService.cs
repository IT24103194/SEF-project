using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data;

public interface ITransactionService
{
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, InventoryItem? Item)> ExecuteStockMovementAtomicAsync(
        Guid inventoryItemId,
        int quantityChange,
        StockMovementType movementType,
        string reason,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, Membership? Membership)> ExecuteMembershipRenewalAtomicAsync(
        Guid memberId,
        Guid planId,
        decimal pricePaid,
        int durationDays,
        Guid? processedByUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, Booking? Booking)> ExecuteBookingAtomicAsync(
        Guid scheduleId,
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, Approval? Approval)> ExecuteRepairApprovalAtomicAsync(
        Guid repairOrderId,
        Guid approverUserId,
        ApprovalDecision decision,
        string? comments,
        decimal approvalThreshold,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> ExecuteAIApprovalAtomicAsync(
        Guid workflowId,
        Guid approverUserId,
        bool approved,
        string comments,
        CancellationToken cancellationToken = default);

    Task LogAuditAsync(
        string entityName,
        string entityId,
        string action,
        Guid? userId,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}

public class TransactionService : ITransactionService
{
    private readonly SmartGymDbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(SmartGymDbContext dbContext, ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction aborted and rolled back due to error.");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction aborted and rolled back due to error.");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<(bool Success, string Message, InventoryItem? Item)> ExecuteStockMovementAtomicAsync(
        Guid inventoryItemId,
        int quantityChange,
        StockMovementType movementType,
        string reason,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync<(bool Success, string Message, InventoryItem? Item)>(async () =>
        {
            var item = await _dbContext.InventoryItems
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);

            if (item == null)
            {
                return (false, "Inventory item not found.", null);
            }

            var newQuantity = item.QuantityInStock + quantityChange;
            if (newQuantity < 0)
            {
                return (false, $"Stock cannot become negative. Insufficient stock for product '{item.Product.Name}'. Current: {item.QuantityInStock}, Requested adjustment: {quantityChange}.", item);
            }

            item.QuantityInStock = newQuantity;
            item.UpdatedAt = DateTime.UtcNow;

            var movement = new StockMovement
            {
                InventoryItemId = item.Id,
                QuantityChange = quantityChange,
                MovementType = movementType,
                Reason = reason,
                PerformedByUserId = performedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.StockMovements.AddAsync(movement, cancellationToken);

            await _dbContext.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "InventoryItem",
                EntityId = item.Id.ToString(),
                Action = $"STOCK_{movementType.ToString().ToUpperInvariant()}",
                UserId = performedByUserId,
                NewValuesJson = $"{{\"QuantityChange\":{quantityChange},\"NewQuantity\":{newQuantity},\"Reason\":\"{reason}\"}}",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (true, $"Stock updated successfully. New balance: {item.QuantityInStock}.", item);
        }, cancellationToken);
    }

    public async Task<(bool Success, string Message, Membership? Membership)> ExecuteMembershipRenewalAtomicAsync(
        Guid memberId,
        Guid planId,
        decimal pricePaid,
        int durationDays,
        Guid? processedByUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync<(bool Success, string Message, Membership? Membership)>(async () =>
        {
            var member = await _dbContext.Members
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);

            if (member == null)
            {
                return (false, "Member not found.", null);
            }

            var plan = await _dbContext.MembershipPlans.FindAsync(new object[] { planId }, cancellationToken);
            if (plan == null)
            {
                return (false, "Membership plan not found.", null);
            }

            // Expire previous active memberships
            var activeMemberships = member.Memberships.Where(ms => ms.Status == MembershipStatus.Active).ToList();
            foreach (var active in activeMemberships)
            {
                active.Status = MembershipStatus.Expired;
                active.UpdatedAt = DateTime.UtcNow;
            }

            var now = DateTime.UtcNow;
            var newMembership = new Membership
            {
                MemberId = member.Id,
                PlanId = plan.Id,
                StartDate = now,
                EndDate = now.AddDays(durationDays),
                Status = MembershipStatus.Active,
                PricePaid = pricePaid,
                AutoRenew = false,
                CreatedAt = now
            };

            await _dbContext.Memberships.AddAsync(newMembership, cancellationToken);

            await _dbContext.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "Membership",
                EntityId = newMembership.Id.ToString(),
                Action = "MEMBERSHIP_RENEWAL",
                UserId = processedByUserId,
                NewValuesJson = $"{{\"PlanId\":\"{plan.Id}\",\"PricePaid\":{pricePaid},\"DurationDays\":{durationDays}}}",
                Timestamp = now
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (true, $"Membership renewed successfully until {newMembership.EndDate:yyyy-MM-dd}.", newMembership);
        }, cancellationToken);
    }

    public async Task<(bool Success, string Message, Booking? Booking)> ExecuteBookingAtomicAsync(
        Guid scheduleId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync<(bool Success, string Message, Booking? Booking)>(async () =>
        {
            var schedule = await _dbContext.ClassSchedules
                .Include(cs => cs.FitnessClass)
                .FirstOrDefaultAsync(cs => cs.Id == scheduleId, cancellationToken);

            if (schedule == null)
            {
                return (false, "Class schedule not found.", null);
            }

            if (schedule.Status != ScheduleStatus.Scheduled)
            {
                return (false, $"Cannot book class with status: {schedule.Status}.", null);
            }

            if (schedule.BookedCount >= schedule.Capacity)
            {
                return (false, "Class is fully booked at maximum capacity.", null);
            }

            // Check if member already booked
            var existingBooking = await _dbContext.Bookings
                .FirstOrDefaultAsync(b => b.ScheduleId == scheduleId && b.MemberId == memberId && b.Status == BookingStatus.Confirmed, cancellationToken);

            if (existingBooking != null)
            {
                return (false, "Member is already booked for this class.", null);
            }

            schedule.BookedCount += 1;
            schedule.UpdatedAt = DateTime.UtcNow;

            var booking = new Booking
            {
                ScheduleId = schedule.Id,
                MemberId = memberId,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Bookings.AddAsync(booking, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (true, "Class booking confirmed successfully.", booking);
        }, cancellationToken);
    }

    public async Task<(bool Success, string Message, Approval? Approval)> ExecuteRepairApprovalAtomicAsync(
        Guid repairOrderId,
        Guid approverUserId,
        ApprovalDecision decision,
        string? comments,
        decimal approvalThreshold,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync<(bool Success, string Message, Approval? Approval)>(async () =>
        {
            var repairOrder = await _dbContext.RepairOrders
                .Include(ro => ro.Approval)
                .Include(ro => ro.Equipment)
                .FirstOrDefaultAsync(ro => ro.Id == repairOrderId, cancellationToken);

            if (repairOrder == null)
            {
                return (false, "Repair order not found.", null);
            }

            if (repairOrder.Approval != null)
            {
                return (false, "Repair order has already been decided.", repairOrder.Approval);
            }

            var approval = new Approval
            {
                RepairOrderId = repairOrder.Id,
                ApproverUserId = approverUserId,
                Decision = decision,
                Comments = comments,
                ApprovalThreshold = approvalThreshold,
                EstimatedCost = repairOrder.EstimatedCost,
                DecidedAt = DateTime.UtcNow
            };

            if (decision == ApprovalDecision.Approved)
            {
                repairOrder.Status = RepairOrderStatus.Approved;
                repairOrder.Equipment.Status = EquipmentStatus.UnderRepair;
            }
            else if (decision == ApprovalDecision.Rejected)
            {
                repairOrder.Status = RepairOrderStatus.Rejected;
            }

            repairOrder.UpdatedAt = DateTime.UtcNow;

            await _dbContext.Approvals.AddAsync(approval, cancellationToken);

            await _dbContext.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "RepairOrder",
                EntityId = repairOrder.Id.ToString(),
                Action = $"REPAIR_{decision.ToString().ToUpperInvariant()}",
                UserId = approverUserId,
                NewValuesJson = $"{{\"Decision\":\"{decision}\",\"Comments\":\"{comments}\",\"Threshold\":{approvalThreshold}}}",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (true, $"Repair order {repairOrder.OrderNumber} {decision.ToString().ToLowerInvariant()} successfully.", approval);
        }, cancellationToken);
    }

    public async Task<(bool Success, string Message)> ExecuteAIApprovalAtomicAsync(
        Guid workflowId,
        Guid approverUserId,
        bool approved,
        string comments,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var workflow = await _dbContext.AIWorkflows
                .Include(w => w.FacilityIssue)
                .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

            if (workflow == null)
            {
                return (false, "AI Workflow not found.");
            }

            workflow.HumanApprovalGranted = approved;
            workflow.Status = approved ? AIWorkflowStatus.Executing : AIWorkflowStatus.Failed;
            workflow.CurrentStep = approved ? "ExecutionApproved" : "RejectedByHuman";
            workflow.UpdatedAt = DateTime.UtcNow;

            if (approved && workflow.FacilityIssue != null)
            {
                workflow.FacilityIssue.Status = FacilityIssueStatus.InRepair;
                workflow.FacilityIssue.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "AIWorkflow",
                EntityId = workflow.Id.ToString(),
                Action = approved ? "AI_WORKFLOW_APPROVED" : "AI_WORKFLOW_REJECTED",
                UserId = approverUserId,
                NewValuesJson = $"{{\"Approved\":{approved},\"Comments\":\"{comments}\"}}",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return (true, $"AI workflow {(approved ? "approved" : "rejected")} successfully.");
        }, cancellationToken);
    }

    public async Task LogAuditAsync(
        string entityName,
        string entityId,
        string action,
        Guid? userId,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
