# SmartGym Relational Entity-Relationship (ER) Model

**Target Database**: PostgreSQL 16 (Relational Engine)  
**ORM Framework**: Entity Framework Core 8 (Npgsql Provider)  
**Total Relational Tables**: 36 tables  
**Integrity**: Full ACID compliance with foreign key constraints and cascade delete/restrict rules.

---

## 1. Complete System Entity-Relationship Diagram

```mermaid
erDiagram
    %% Identity & Access Control
    users ||--o{ user_roles : "has"
    roles ||--o{ user_roles : "assigned_to"
    users ||--o{ refresh_tokens : "owns"
    users ||--o| members : "profile"

    %% Membership & Subscriptions
    membership_plans ||--o{ memberships : "defines"
    members ||--o{ memberships : "subscribes"
    members ||--o{ goals : "sets"
    members ||--o{ progress_records : "logs"

    %% Fitness Classes & Bookings
    locations ||--o{ equipment : "contains"
    locations ||--o{ class_schedules : "hosts"
    class_categories ||--o{ fitness_classes : "categorizes"
    fitness_classes ||--o{ class_schedules : "scheduled_as"
    users ||--o{ class_schedules : "instructed_by"
    members ||--o{ bookings : "reserves"
    class_schedules ||--o{ bookings : "booked_in"
    bookings ||--o| attendances : "verified_by"

    %% Inventory & Supplements
    product_categories ||--o{ products : "categorizes"
    products ||--o| inventory_items : "stocked_as"
    inventory_items ||--o{ stock_movements : "tracked_by"
    suppliers ||--o{ purchase_orders : "supplies"
    purchase_orders ||--o{ purchase_order_items : "contains"
    products ||--o{ purchase_order_items : "ordered_in"

    %% Facility Maintenance & AI Workflows
    members ||--o{ facility_issues : "reports"
    equipment ||--o{ facility_issues : "impacted_in"
    locations ||--o{ facility_issues : "occurs_at"
    facility_issues ||--o{ issue_images : "evidenced_by"
    facility_issues ||--o{ repair_orders : "resolved_by"
    repair_orders ||--o{ repair_order_items : "specifies"
    products ||--o{ repair_order_items : "utilizes"

    %% AI Multi-Agent & Approvals
    facility_issues ||--o{ ai_workflows : "triggers"
    ai_workflows ||--o{ ai_workflow_steps : "executes"
    ai_workflow_steps ||--o{ ai_validation_results : "validates"
    ai_workflow_steps ||--o{ ai_tool_executions : "runs"
    ai_workflows ||--o{ approvals : "gated_by"
    users ||--o{ approvals : "reviewed_by"

    %% Auditing & Notifications
    users ||--o{ notifications : "receives"
    users ||--o{ feedbacks : "submits"
    users ||--o{ audit_logs : "performed_by"

    %% Table Attribute Summaries
    users {
        uuid Id PK
        string Email UK
        string PasswordHash
        string FirstName
        string LastName
        datetime CreatedAt
    }
    facility_issues {
        uuid Id PK
        uuid ReportedByMemberId FK
        uuid EquipmentId FK
        uuid LocationId FK
        string Title
        string Description
        int Severity
        string Status
        datetime ReportedAt
    }
    ai_workflows {
        uuid Id PK
        uuid FacilityIssueId FK
        string WorkflowType
        string Status
        string CurrentStep
        float EstimatedCost
        boolean RequiresHumanApproval
        string CorrelationId
    }
    approvals {
        uuid Id PK
        uuid AIWorkflowId FK
        uuid ReviewedByUserId FK
        string ActionType
        string Status
        float CostEstimate
        datetime ReviewedAt
    }
    inventory_items {
        uuid Id PK
        uuid ProductId FK
        int QuantityInStock
        int ReorderThreshold
        int MaxStockLevel
        string LocationBin
    }
    repair_orders {
        uuid Id PK
        uuid FacilityIssueId FK
        uuid SupplierId FK
        float EstimatedCost
        string Status
        string TechnicianNotes
    }
```

---

## 2. Table-by-Table Architectural Schema

### 2.1 Identity & Access Control
1. **`users`**: Central account identity store containing credentials and timestamps.
   - Primary Key: `Id` (UUID)
   - Unique Index: `Email` (Case-insensitive unique)
2. **`roles`**: System permission tiers (`Admin`, `Manager`, `Trainer`, `Member`).
   - Primary Key: `Id` (UUID)
   - Unique Index: `Name`
3. **`user_roles`**: Many-to-Many junction between `users` and `roles`.
   - Composite PK: `(UserId, RoleId)`
   - Foreign Keys: `FK_user_roles_users_UserId`, `FK_user_roles_roles_RoleId` (Cascade Delete)
4. **`refresh_tokens`**: Revocable JWT session tokens.
   - Primary Key: `Id` (UUID)
   - Foreign Key: `FK_refresh_tokens_users_UserId` (Cascade Delete)
   - Columns: `Token`, `ExpiresAt`, `RevokedAt`, `ReplacedByToken`

### 2.2 Facility Maintenance & Multi-Agent AI Subsystem
5. **`facility_issues`**: Primary equipment problem registry.
   - Primary Key: `Id` (UUID)
   - Foreign Keys:
     - `FK_facility_issues_members_ReportedByMemberId` (Restrict Delete)
     - `FK_facility_issues_equipment_EquipmentId` (Set Null)
     - `FK_facility_issues_locations_LocationId` (Set Null)
   - Columns: `Title`, `Description`, `Severity`, `Status`, `ReportedAt`, `ResolvedAt`, `SanitizedDescription`, `ModerationStatus`
6. **`issue_images`**: Evidence photographs uploaded by members.
   - Primary Key: `Id` (UUID)
   - Foreign Key: `FK_issue_images_facility_issues_FacilityIssueId` (Cascade Delete)
7. **`ai_workflows`**: State persistence for LangGraph multi-agent execution runs.
   - Primary Key: `Id` (UUID)
   - Foreign Key: `FK_ai_workflows_facility_issues_FacilityIssueId` (Cascade Delete)
   - Columns: `WorkflowType`, `Status`, `CurrentStep`, `EstimatedCost`, `RequiresHumanApproval`, `CorrelationId`
8. **`ai_workflow_steps`**: Chronological record of agent invocations (`Safety`, `Planner`, `Domain`, `Action`).
   - Primary Key: `Id` (UUID)
   - Foreign Key: `FK_ai_workflow_steps_ai_workflows_AIWorkflowId` (Cascade Delete)
9. **`ai_tool_executions`**: Auditable record of every tool executed by AI agents.
   - Primary Key: `Id` (UUID)
   - Foreign Key: `FK_ai_tool_executions_ai_workflow_steps_AIWorkflowStepId` (Cascade Delete)
   - Columns: `ToolName`, `InputParametersJson`, `OutputResultJson`, `ExecutionTimeMs`, `IsSuccess`
10. **`ai_validation_results`**: Deterministic rule outcomes from `SafetyValidationAgent`.
    - Primary Key: `Id` (UUID)
    - Foreign Key: `FK_ai_validation_results_ai_workflow_steps_AIWorkflowStepId` (Cascade Delete)
11. **`approvals`**: Human-in-the-Loop decision records.
    - Primary Key: `Id` (UUID)
    - Foreign Keys:
      - `FK_approvals_ai_workflows_AIWorkflowId` (Cascade Delete)
      - `FK_approvals_users_ReviewedByUserId` (Restrict Delete)
    - Columns: `ActionType`, `Status`, `CostEstimate`, `DecisionNotes`, `ReviewedAt`

### 2.3 Inventory, Supplements & Suppliers
12. **`product_categories`**: Hierarchical supplement categories (Proteins, Creatines, Vitamins).
13. **`products`**: Retail supplement items with SKU, brand, and retail price.
14. **`inventory_items`**: Stock physical tracking records (`QuantityInStock`, `ReorderThreshold`, `LocationBin`).
15. **`stock_movements`**: Immutable audit logs of every addition, deduction, or adjustment.
16. **`suppliers`**: Certified parts and supplement vendor directory.
17. **`purchase_orders`**: Procurement orders placed with suppliers.
18. **`purchase_order_items`**: Itemized breakdown of supplier orders.

### 2.4 Fitness Operations & Bookings
19. **`locations`**: Physical gym rooms and functional workout zones.
20. **`equipment`**: Catalog of gym machines, serial numbers, and maintenance histories.
21. **`class_categories`**: Fitness genres (Cardio, HIIT, Strength, Yoga).
22. **`fitness_classes`**: Reusable class definitions and max capacities.
23. **`class_schedules`**: Concrete timetable slots with date, start/end time, and assigned trainer.
24. **`bookings`**: Member reservations with capacity guardrails.
25. **`attendances`**: Verified trainer check-in records.

### 2.5 Memberships, Goals & Communications
26. **`members`**: Member profiles linked 1-to-1 with `users`.
27. **`membership_plans`**: Subscription definitions (Bronze, Silver, Gold, Platinum).
28. **`memberships`**: Active subscription contracts with start and expiry dates.
29. **`goals`**: Member fitness targets (Weight, Body Fat %, Muscle Mass).
30. **`progress_records`**: Chronological milestone measurement logs.
31. **`feedbacks`**: Member suggestions and ratings with manager moderation notes.
32. **`notifications`**: Targeted user alerts across Web and Mobile.
33. **`audit_logs`**: System-wide administrative action logs with IP address and timestamps.
34. **`repair_orders`**: Operational maintenance dispatch orders.
35. **`repair_order_items`**: Itemized parts required for equipment repair.
36. **`__EFMigrationsHistory`**: Entity Framework migration tracking table.
