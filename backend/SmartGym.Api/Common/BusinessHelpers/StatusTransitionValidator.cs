using SmartGym.Api.Entities;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Common.BusinessHelpers;

public static class StatusTransitionValidator
{
    private static readonly Dictionary<FacilityIssueStatus, HashSet<FacilityIssueStatus>> IssueTransitions = new()
    {
        [FacilityIssueStatus.SUBMITTED] = new() { FacilityIssueStatus.AI_ANALYZING, FacilityIssueStatus.PENDING_APPROVAL, FacilityIssueStatus.IN_PROGRESS, FacilityIssueStatus.REJECTED },
        [FacilityIssueStatus.AI_ANALYZING] = new() { FacilityIssueStatus.PENDING_APPROVAL, FacilityIssueStatus.APPROVED, FacilityIssueStatus.REJECTED, FacilityIssueStatus.REVISION_REQUIRED },
        [FacilityIssueStatus.PENDING_APPROVAL] = new() { FacilityIssueStatus.APPROVED, FacilityIssueStatus.REJECTED, FacilityIssueStatus.REVISION_REQUIRED },
        [FacilityIssueStatus.APPROVED] = new() { FacilityIssueStatus.VENDOR_CONTACTED, FacilityIssueStatus.REPAIR_SCHEDULED, FacilityIssueStatus.IN_PROGRESS, FacilityIssueStatus.RESOLVED },
        [FacilityIssueStatus.VENDOR_CONTACTED] = new() { FacilityIssueStatus.REPAIR_SCHEDULED, FacilityIssueStatus.IN_PROGRESS, FacilityIssueStatus.RESOLVED },
        [FacilityIssueStatus.REPAIR_SCHEDULED] = new() { FacilityIssueStatus.IN_PROGRESS, FacilityIssueStatus.RESOLVED },
        [FacilityIssueStatus.IN_PROGRESS] = new() { FacilityIssueStatus.RESOLVED, FacilityIssueStatus.REVISION_REQUIRED },
        [FacilityIssueStatus.REVISION_REQUIRED] = new() { FacilityIssueStatus.PENDING_APPROVAL, FacilityIssueStatus.APPROVED, FacilityIssueStatus.REJECTED },
        [FacilityIssueStatus.RESOLVED] = new(), // Terminal
        [FacilityIssueStatus.REJECTED] = new()  // Terminal
    };

    private static readonly Dictionary<BookingStatus, HashSet<BookingStatus>> BookingTransitions = new()
    {
        [BookingStatus.Confirmed] = new() { BookingStatus.Cancelled },
        [BookingStatus.Waitlisted] = new() { BookingStatus.Confirmed, BookingStatus.Cancelled },
        [BookingStatus.Cancelled] = new()
    };

    private static readonly Dictionary<MembershipStatus, HashSet<MembershipStatus>> MembershipTransitions = new()
    {
        [MembershipStatus.Active] = new() { MembershipStatus.Expired, MembershipStatus.Cancelled },
        [MembershipStatus.Expired] = new() { MembershipStatus.Active }, // via renewal
        [MembershipStatus.Cancelled] = new() { MembershipStatus.Active } // via renewal
    };

    private static readonly Dictionary<PurchaseOrderStatus, HashSet<PurchaseOrderStatus>> PurchaseOrderTransitions = new()
    {
        [PurchaseOrderStatus.Draft] = new() { PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Cancelled },
        [PurchaseOrderStatus.Submitted] = new() { PurchaseOrderStatus.Approved, PurchaseOrderStatus.Cancelled },
        [PurchaseOrderStatus.Approved] = new() { PurchaseOrderStatus.Received, PurchaseOrderStatus.Cancelled },
        [PurchaseOrderStatus.Received] = new(),
        [PurchaseOrderStatus.Cancelled] = new()
    };

    public static void ValidateIssueTransition(FacilityIssueStatus current, FacilityIssueStatus target)
    {
        if (current == target) return;
        if (!IssueTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
        {
            throw new BadRequestException($"Invalid issue status transition from '{current}' to '{target}'.");
        }
    }

    public static void ValidateBookingTransition(BookingStatus current, BookingStatus target)
    {
        if (current == target) return;
        if (!BookingTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
        {
            throw new BadRequestException($"Invalid booking status transition from '{current}' to '{target}'.");
        }
    }

    public static void ValidateMembershipTransition(MembershipStatus current, MembershipStatus target)
    {
        if (current == target) return;
        if (!MembershipTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
        {
            throw new BadRequestException($"Invalid membership status transition from '{current}' to '{target}'.");
        }
    }

    public static void ValidatePurchaseOrderTransition(PurchaseOrderStatus current, PurchaseOrderStatus target)
    {
        if (current == target) return;
        if (!PurchaseOrderTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
        {
            throw new BadRequestException($"Invalid purchase order status transition from '{current}' to '{target}'.");
        }
    }
}
