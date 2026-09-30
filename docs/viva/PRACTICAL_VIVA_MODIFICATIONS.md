# Practical Viva Defense Live Modifications Guide

This guide provides step-by-step instructions, file locations, exact code diffs, and verification commands for the **8 practical modification tasks** commonly requested by university examiners during viva defense.

---

## Task 1: Change Repair Approval Cost Threshold

**Examiner Prompt**: *"Change the financial threshold that triggers human manager approval from LKR 10,000 to LKR 5,000 (or LKR 25,000)."*

### File to Modify
`ai-service/smartgym_ai/configuration/settings.py` (Line ~20)

### Code Modification
```python
# Before:
AI_APPROVAL_COST_THRESHOLD: float = 10000.0

# After:
AI_APPROVAL_COST_THRESHOLD: float = 5000.0  # Or 25000.0 as requested
```

### Verification
Run the Golden Case 3 test:
```bash
pytest tests/ai/test_complete_workflow.py -k "test_golden_case_3"
```
*Result*: Workflows with repair costs above LKR 5,000 now route to `AwaitingApproval`.

---

## Task 2: Add Booking Capacity Rule

**Examiner Prompt**: *"Add a business rule preventing members from booking a fitness class if fewer than 2 spots remain (or if the class begins within 1 hour)."*

### File to Modify
`backend/SmartGym.Api/Services/BookingService.cs` (Inside `CreateBookingAsync`)

### Code Modification
```csharp
// Locate the capacity check inside CreateBookingAsync:
var schedule = await _db.ClassSchedules
    .Include(s => s.Bookings)
    .Include(s => s.FitnessClass)
    .FirstOrDefaultAsync(s => s.Id == request.ClassScheduleId);

// Add custom viva rule:
int remainingSpots = schedule.FitnessClass.MaxCapacity - schedule.Bookings.Count(b => b.Status != BookingStatus.Cancelled);
if (remainingSpots <= 2)
{
    throw new InvalidOperationException("Booking rejected: Classes with 2 or fewer spots remaining are reserved for premium members.");
}
```

### Verification
```bash
dotnet test tests/backend/SmartGym.Api.Tests --filter "FullyQualifiedName~Bookings"
```

---

## Task 3: Add Inventory Restock Validation Rule

**Examiner Prompt**: *"Add a validation rule requiring that all inventory restocks must be in multiples of 5, or have a minimum quantity of 10 units."*

### File to Modify
`backend/SmartGym.Api/DTOs/Inventory/InventoryAdjustmentRequest.cs` (Validator class)

### Code Modification
```csharp
public class InventoryAdjustmentRequestValidator : AbstractValidator<InventoryAdjustmentRequest>
{
    public InventoryAdjustmentRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        // Custom Viva Rule:
        RuleFor(x => x.Quantity)
            .Must(q => q >= 10 && q % 5 == 0)
            .WithMessage("Viva Rule: Restock quantity must be at least 10 units and in multiples of 5.");
    }
}
```

### Verification
```bash
dotnet test tests/backend/SmartGym.Api.Tests --filter "FullyQualifiedName~Inventory"
```

---

## Task 4: Change Membership Renewal Rule

**Examiner Prompt**: *"Modify the membership renewal rule so that memberships expired for more than 30 days cannot be renewed online and must visit the front desk."*

### File to Modify
`backend/SmartGym.Api/Services/MembershipService.cs` (Inside `RenewMembershipAsync`)

### Code Modification
```csharp
// Inside RenewMembershipAsync:
var existingMembership = await _db.Memberships
    .FirstOrDefaultAsync(m => m.MemberId == memberId && m.Status == MembershipStatus.Active);

if (existingMembership != null && existingMembership.EndDate < DateTime.UtcNow.AddDays(-30))
{
    throw new InvalidOperationException("Membership has been expired for over 30 days. Please visit the front desk for reactivation.");
}
```

### Verification
```bash
dotnet test tests/backend/SmartGym.Api.Tests --filter "FullyQualifiedName~Memberships"
```

---

## Task 5: Disable One Agent Tool (Simulate Tool Deprecation / Outage)

**Examiner Prompt**: *"Disable the `checkInventory` tool in the Gym Domain Analysis Agent and prove that the agent handles the missing tool gracefully."*

### File to Modify
`ai-service/smartgym_ai/agents/domain_analysis_agent.py`

### Code Modification
```python
# In DomainAnalysisAgent.__init__ or analyze_facility_issue:
# Temporarily comment out checkInventory registration:
self.tools = {
    "getEquipmentDetails": GetEquipmentDetailsTool(),
    # "checkInventory": CheckInventoryTool(),  # DISABLED FOR VIVA DEMO
    "getSupplierDetails": GetSupplierDetailsTool(),
}

# Update analyze_facility_issue tool invocation:
if "checkInventory" in self.tools:
    part_record = await self.tools["checkInventory"].execute(...)
else:
    # Fallback when inventory tool is offline
    part_available = False
    notes = "Inventory service offline; assuming part requires supplier order."
```

### Verification
```bash
pytest tests/ai/test_domain_analysis_agent.py
```
*Result*: Domain agent reports `part_available = False` and recommends vendor procurement without failing.

---

## Task 6: Modify AI Validation Rule (New Safety Keyword)

**Examiner Prompt**: *"Add a new safety validation keyword (e.g. 'lawsuit' or 'injury hazard') that immediately flags the ticket for mandatory manager review regardless of repair cost."*

### File to Modify
`ai-service/smartgym_ai/agents/safety_validation_agent.py`

### Code Modification
```python
# Inside SafetyValidationAgent.validate_request:
HIGH_RISK_KEYWORDS = ["lawsuit", "injury hazard", "hospital", "fracture", "electrocution"]

description_lower = safety_input.facility_issue.get("description", "").lower()
if any(keyword in description_lower for keyword in HIGH_RISK_KEYWORDS):
    # Enforce mandatory approval regardless of cost:
    output.approval_required = True
    output.issues.append("High-risk liability keyword detected: mandatory manager review enforced.")
```

### Verification
Run safety agent test:
```bash
pytest tests/ai/test_safety_agent.py
```

---

## Task 7: Debug AI Workflow Failure (Simulate Database Foreign Key Error)

**Examiner Prompt**: *"Simulate an unexpected database foreign key failure during repair order creation, and demonstrate that the system falls back to `_safe_failure_node` without unhandled crashes."*

### File to Modify
`ai-service/smartgym_ai/tools/action_tools.py` (Inside `CreateRepairOrderTool.execute`)

### Code Modification
```python
# Simulate intentional failure:
async def execute(self, params: dict[str, Any]) -> ToolResult:
    # VIVA DEMO: Force database constraint failure
    raise Exception("Simulated PostgreSQL foreign key violation: FK_repair_orders_facility_issues")
```

### Verification
Run the safe failure test:
```bash
pytest tests/ai/test_safe_failure.py
```
*Result*: The LangGraph engine catches the exception, transitions to status `Failed`, assigns the ticket to on-site human review, and logs the incident cleanly.

---

## Task 8: Modify Notification Dispatch Behavior

**Examiner Prompt**: *"Change the notification behavior so that whenever a repair order is approved, an urgent in-app alert is generated for all managers."*

### File to Modify
`backend/SmartGym.Api/Services/ApprovalService.cs` (Inside `ExecuteApprovalDecisionAsync`)

### Code Modification
```csharp
// When action == "Approve":
var notification = new Notification
{
    Id = Guid.NewGuid(),
    UserId = approval.ReviewedByUserId,
    Title = "URGENT: Facility Repair Order Authorized",
    Message = $"Repair order for issue '{approval.AIWorkflow.FacilityIssue.Title}' has been approved and dispatched to supplier.",
    NotificationType = NotificationType.UrgentAlert,
    CreatedAt = DateTime.UtcNow,
    IsRead = false
};
_db.Notifications.Add(notification);
await _db.SaveChangesAsync();
```

### Verification
```bash
dotnet test tests/backend/SmartGym.Api.Tests --filter "FullyQualifiedName~Approvals"
```
