# SmartGym PostgreSQL Database Schema Specification

## 1. Architectural Overview

The SmartGym database is built on **PostgreSQL 18** using **Entity Framework Core 8** (Npgsql provider). It enforces a strict **Third Normal Form (3NF)** relational structure that balances relational integrity, query performance, and the structured persistence requirements of the Agentic AI diagnostics engine.

### Core Design Principles

1. **Deterministic Primary Keys:** All entity primary keys use UUID (Guid) to support distributed generation, prevent ID enumeration attacks, and simplify disconnected client sync.
2. **PostgreSQL Idiomatic Naming:** Table names use snake_case (`users`, `memberships`, `ai_workflows`) matching standard PostgreSQL conventions.
3. **Time Zone Integrity:** All temporal fields are stored as `timestamp with time zone` (`timestamptz`). EF Core applies a universal UTC value converter on all `DateTime` and `DateTime?` properties.
4. **Auditability:** Entities implement automatic auditing. `CreatedAt` is populated automatically on insertion, and `UpdatedAt` is updated on every mutation via `SmartGymDbContext.SaveChanges()` and `SaveChangesAsync()`.
5. **Data Protection & Privacy:**
   - **No Plaintext Secrets:** Passwords are salted and hashed using **BCrypt** (work factor 11).
   - **No API Keys or JWT Secrets:** System configuration secrets reside strictly in environment variables.
   - **Structured AI State Only:** The AI domain stores structured outputs (`diagnosis_summary`, `recommended_action`, `confidence_score`, execution times, and JSON payloads of final conclusions). Hidden internal reasoning and chain-of-thought scratchpads are never persisted to the database.

---

## 2. Global Constraint & Indexing Strategy

### Standard Check Constraints
- `CK_membership_plans_price`: `Price >= 0`
- `CK_membership_plans_duration`: `DurationDays > 0`
- `CK_memberships_price_paid`: `PricePaid >= 0`
- `CK_fitness_classes_capacity`: `DefaultCapacity > 0`
- `CK_fitness_classes_duration`: `DurationMinutes > 0`
- `CK_class_schedules_capacity`: `Capacity > 0`
- `CK_class_schedules_booked_count`: `BookedCount >= 0`
- `CK_products_unit_price`: `UnitPrice >= 0`
- `CK_products_cost_price`: `CostPrice >= 0`
- `CK_inventory_items_quantity`: `QuantityInStock >= 0`
- `CK_inventory_items_reorder_threshold`: `ReorderThreshold >= 0`
- `CK_inventory_items_max_stock`: `MaxStockLevel >= ReorderThreshold`
- `CK_purchase_orders_total_amount`: `TotalAmount >= 0`
- `CK_purchase_order_items_quantity`: `Quantity > 0`
- `CK_purchase_order_items_unit_cost`: `UnitCost >= 0`
- `CK_purchase_order_items_total_cost`: `TotalCost >= 0`
- `CK_feedbacks_rating`: `Rating BETWEEN 1 AND 5`
- `CK_repair_orders_estimated_cost`: `EstimatedCost >= 0`
- `CK_repair_orders_actual_cost`: `ActualCost IS NULL OR ActualCost >= 0`
- `CK_repair_order_items_quantity`: `Quantity > 0`
- `CK_repair_order_items_unit_cost`: `UnitCost >= 0`
- `CK_repair_order_items_total_cost`: `TotalCost >= 0`
- `CK_approvals_threshold`: `ApprovalThreshold >= 0`
- `CK_approvals_estimated_cost`: `EstimatedCost >= 0`

### Indexing Matrix
| Index Name | Table | Columns | Type / Purpose |
| :--- | :--- | :--- | :--- |
| `IX_users_Email` | `users` | `Email` | Unique / Fast auth lookup |
| `IX_users_IsActive` | `users` | `IsActive` | Filtered queries |
| `IX_roles_Name` | `roles` | `Name` | Unique / Role lookup |
| `IX_members_UserId` | `members` | `UserId` | Unique 1:1 user-member |
| `IX_memberships_MemberId_Status` | `memberships` | `MemberId, Status` | Active subscription queries |
| `IX_class_schedules_StartTime_EndTime` | `class_schedules` | `StartTime, EndTime` | Weekly timetable rendering |
| `IX_bookings_ScheduleId_MemberId` | `bookings` | `ScheduleId, MemberId` | Prevent duplicate active booking |
| `IX_bookings_BookingDate` | `bookings` | `BookingTime` | Booking history filtering |
| `IX_products_SKU` | `products` | `SKU` | Unique / Barcode & stock lookups |
| `IX_inventory_items_ProductId` | `inventory_items` | `ProductId` | Unique 1:1 stock tracking |
| `IX_equipment_SerialNumber` | `equipment` | `SerialNumber` | Unique / Asset identifier |
| `IX_facility_issues_Status` | `facility_issues` | `Status` | Triage & AI workflow triggers |
| `IX_ai_workflows_IssueId` | `ai_workflows` | `IssueId` | Unique 1:1 issue diagnostics |
| `IX_ai_workflows_Status` | `ai_workflows` | `Status` | Agent pipeline status |
| `IX_approvals_RepairOrderId` | `approvals` | `RepairOrderId` | Unique 1:1 repair approval |
| `IX_audit_logs_EntityName_EntityId` | `audit_logs` | `EntityName, EntityId` | Entity audit history trail |

---

## 3. Data Dictionary by Functional Domain

### 3.1 Identity Domain (4 Tables)

#### `users`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Email` | varchar(256) | No | UNIQUE INDEX |
| `PasswordHash` | varchar(512) | No | BCrypt hashed password |
| `FirstName` | varchar(100) | No | |
| `LastName` | varchar(100) | No | |
| `PhoneNumber` | varchar(30) | Yes | |
| `IsActive` | boolean | No | Default `true`, INDEX |
| `CreatedAt` | timestamptz | No | Auto-audit, INDEX |
| `UpdatedAt` | timestamptz | Yes | Auto-audit |

#### `roles`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Name` | varchar(50) | No | UNIQUE INDEX (`Admin`, `Trainer`, `Member`) |
| `Description` | varchar(250) | Yes | |
| `CreatedAt` | timestamptz | No | |

#### `user_roles`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `UserId` | uuid | No | PRIMARY KEY, FK -> `users.Id` (CASCADE) |
| `RoleId` | uuid | No | PRIMARY KEY, FK -> `roles.Id` (CASCADE) |
| `AssignedAt` | timestamptz | No | |

#### `refresh_tokens`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `UserId` | uuid | No | FK -> `users.Id` (CASCADE) |
| `Token` | varchar(512) | No | Cryptographically secure token, INDEX |
| `ExpiresAt` | timestamptz | No | INDEX |
| `IsRevoked` | boolean | No | Default `false` |
| `ReplacedByToken` | varchar(512) | Yes | Token rotation tracking |
| `CreatedAt` | timestamptz | No | |

---

### 3.2 Membership Domain (5 Tables)

#### `members`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `UserId` | uuid | No | UNIQUE INDEX, FK -> `users.Id` (CASCADE) |
| `EmergencyContactName` | varchar(150) | Yes | |
| `EmergencyContactPhone` | varchar(50) | Yes | |
| `DateOfBirth` | timestamptz | Yes | |
| `Gender` | varchar(20) | Yes | |
| `Address` | varchar(250) | Yes | |
| `MedicalConditions` | varchar(1000) | Yes | Medical safety notes |
| `JoinDate` | timestamptz | No | INDEX |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `membership_plans`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Name` | varchar(100) | No | UNIQUE INDEX (e.g. "Gold Tier") |
| `Description` | varchar(500) | Yes | |
| `Price` | numeric(10,2) | No | CHECK (`Price >= 0`) |
| `DurationDays` | integer | No | CHECK (`DurationDays > 0`) |
| `MaxClassesPerWeek`| integer | No | |
| `HasTrainerAccess` | boolean | No | Default `false` |
| `IsActive` | boolean | No | INDEX |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `memberships`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `MemberId` | uuid | No | FK -> `members.Id` (CASCADE) |
| `PlanId` | uuid | No | FK -> `membership_plans.Id` (RESTRICT) |
| `StartDate` | timestamptz | No | |
| `EndDate` | timestamptz | No | INDEX |
| `Status` | integer | No | Enum (`Active=1`, `Expired=2`, `Cancelled=3`) |
| `AutoRenew` | boolean | No | Default `false` |
| `PricePaid` | numeric(10,2) | No | CHECK (`PricePaid >= 0`) |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `goals`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `MemberId` | uuid | No | FK -> `members.Id` (CASCADE) |
| `Title` | varchar(200) | No | |
| `TargetValue` | numeric(10,2) | No | e.g. 100.00 |
| `CurrentValue` | numeric(10,2) | No | e.g. 85.00 |
| `Unit` | varchar(20) | No | e.g. "kg", "%" |
| `TargetDate` | timestamptz | No | INDEX |
| `Status` | integer | No | Enum (`InProgress=1`, `Achieved=2`, `Abandoned=3`) |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `progress_records`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `GoalId` | uuid | No | FK -> `goals.Id` (CASCADE) |
| `RecordedDate` | timestamptz | No | INDEX with GoalId |
| `Value` | numeric(10,2) | No | |
| `Notes` | varchar(500) | Yes | |
| `RecordedByTrainerId` | uuid | Yes | FK reference to User Trainer |
| `CreatedAt` | timestamptz | No | |

---

### 3.3 Classes Domain (5 Tables)

#### `class_categories`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Name` | varchar(100) | No | UNIQUE INDEX (e.g. "HIIT") |
| `Description` | varchar(500) | Yes | |
| `CreatedAt` | timestamptz | No | |

#### `fitness_classes`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `CategoryId` | uuid | No | FK -> `class_categories.Id` (RESTRICT) |
| `Name` | varchar(150) | No | INDEX |
| `Description` | varchar(1000)| Yes | |
| `DurationMinutes`| integer | No | CHECK (`DurationMinutes > 0`) |
| `DefaultCapacity`| integer | No | CHECK (`DefaultCapacity > 0`) |
| `IntensityLevel` | varchar(50) | No | e.g. "High", "Medium" |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `class_schedules`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `ClassId` | uuid | No | FK -> `fitness_classes.Id` (CASCADE) |
| `TrainerId` | uuid | No | FK -> `users.Id` (RESTRICT) |
| `Room` | varchar(100) | No | e.g. "Studio 1" |
| `StartTime` | timestamptz | No | COMPOSITE INDEX with EndTime |
| `EndTime` | timestamptz | No | |
| `Capacity` | integer | No | CHECK (`Capacity > 0`) |
| `BookedCount` | integer | No | CHECK (`BookedCount >= 0`) |
| `Status` | integer | No | Enum (`Scheduled=1`, `Completed=2`, `Cancelled=3`) |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `bookings`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `ScheduleId` | uuid | No | FK -> `class_schedules.Id` (CASCADE) |
| `MemberId` | uuid | No | FK -> `members.Id` (RESTRICT) |
| `BookingTime` | timestamptz | No | INDEX |
| `Status` | integer | No | Enum (`Confirmed=1`, `Cancelled=2`, `Waitlisted=3`) |
| `CancelledAt` | timestamptz | Yes | |
| `CancellationReason` | varchar(500) | Yes | |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `attendances`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `BookingId` | uuid | No | UNIQUE INDEX, FK -> `bookings.Id` (CASCADE) |
| `ScheduleId` | uuid | No | FK -> `class_schedules.Id` (RESTRICT) |
| `MemberId` | uuid | No | FK -> `members.Id` (RESTRICT) |
| `CheckedInAt` | timestamptz | No | INDEX |
| `Status` | integer | No | Enum (`Attended=1`, `Absent=2`, `Excused=3`) |
| `MarkedByUserId`| uuid | Yes | Staff member who marked attendance |
| `CreatedAt` | timestamptz | No | |

---

### 3.4 Inventory Domain (7 Tables)

#### `suppliers`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Name` | varchar(150) | No | INDEX |
| `ContactPerson` | varchar(100) | Yes | |
| `Email` | varchar(256) | Yes | |
| `Phone` | varchar(50) | Yes | |
| `Address` | varchar(300) | Yes | |
| `IsActive` | boolean | No | Default `true`, INDEX |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `product_categories`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Name` | varchar(100) | No | UNIQUE INDEX |
| `Description` | varchar(500) | Yes | |
| `CreatedAt` | timestamptz | No | |

#### `products`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `CategoryId` | uuid | No | FK -> `product_categories.Id` (RESTRICT) |
| `SupplierId` | uuid | No | FK -> `suppliers.Id` (RESTRICT) |
| `SKU` | varchar(50) | No | UNIQUE INDEX |
| `Name` | varchar(150) | No | INDEX |
| `Description` | varchar(1000)| Yes | |
| `UnitPrice` | numeric(10,2) | No | CHECK (`UnitPrice >= 0`) |
| `CostPrice` | numeric(10,2) | No | CHECK (`CostPrice >= 0`) |
| `ExpiryDate` | timestamptz | Yes | |
| `IsActive` | boolean | No | INDEX |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `inventory_items`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `ProductId` | uuid | No | UNIQUE INDEX, FK -> `products.Id` (CASCADE) |
| `QuantityInStock`| integer | No | CHECK (`QuantityInStock >= 0`) |
| `ReorderThreshold`| integer| No | CHECK (`ReorderThreshold >= 0`) |
| `MaxStockLevel` | integer | No | CHECK (`MaxStockLevel >= ReorderThreshold`) |
| `LocationBin` | varchar(100) | Yes | |
| `LastRestockedAt`| timestamptz| Yes | |
| `UpdatedAt` | timestamptz | No | |

#### `stock_movements`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `InventoryItemId`| uuid | No | FK -> `inventory_items.Id` (CASCADE) |
| `QuantityChange` | integer | No | Positive (inbound) or negative (outbound) |
| `MovementType` | integer | No | Enum (`Restock=1`, `Sale=2`, `Adjustment=3`, `Waste=4`) |
| `Reason` | varchar(250) | Yes | |
| `PerformedByUserId`| uuid | Yes | Staff member who initiated adjustment |
| `CreatedAt` | timestamptz | No | COMPOSITE INDEX with InventoryItemId |

#### `purchase_orders`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `SupplierId` | uuid | No | FK -> `suppliers.Id` (RESTRICT) |
| `OrderNumber` | varchar(50) | No | UNIQUE INDEX |
| `TotalAmount` | numeric(12,2) | No | CHECK (`TotalAmount >= 0`) |
| `Status` | integer | No | Enum (`Draft=1`, `Submitted=2`, `Approved=3`, `Received=4`) |
| `OrderedAt` | timestamptz | No | INDEX |
| `ExpectedDeliveryDate`| timestamptz| Yes | |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `purchase_order_items`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `PurchaseOrderId`| uuid | No | FK -> `purchase_orders.Id` (CASCADE) |
| `ProductId` | uuid | No | FK -> `products.Id` (RESTRICT) |
| `Quantity` | integer | No | CHECK (`Quantity > 0`) |
| `UnitCost` | numeric(10,2) | No | CHECK (`UnitCost >= 0`) |
| `TotalCost` | numeric(12,2) | No | CHECK (`TotalCost >= 0`) |

---

### 3.5 Facility & Maintenance Domain (8 Tables)

#### `locations`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `Name` | varchar(100) | No | INDEX (e.g. "Cardio Zone A") |
| `Floor` | varchar(50) | Yes | e.g. "Ground Floor" |
| `Description` | varchar(500) | Yes | |
| `CreatedAt` | timestamptz | No | |

#### `equipment`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `LocationId` | uuid | No | FK -> `locations.Id` (RESTRICT) |
| `SerialNumber`| varchar(100) | No | UNIQUE INDEX |
| `Name` | varchar(150) | No | |
| `Model` | varchar(100) | Yes | |
| `Manufacturer` | varchar(100) | Yes | |
| `PurchaseDate` | timestamptz | No | INDEX |
| `WarrantyExpiryDate`| timestamptz| Yes | |
| `Status` | integer | No | Enum (`Operational=1`, `NeedsMaintenance=2`, `OutOfService=3`, `UnderRepair=4`) |
| `LastServicedDate`| timestamptz| Yes | |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `feedbacks`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `MemberId` | uuid | No | FK -> `members.Id` (RESTRICT) |
| `Subject` | varchar(200) | No | |
| `Content` | varchar(2000)| No | |
| `Rating` | integer | No | CHECK (`Rating BETWEEN 1 AND 5`), INDEX |
| `Status` | integer | No | Enum (`Pending=1`, `Reviewed=2`, `Moderated=3`, `Actioned=4`) |
| `AdminResponse`| text | Yes | |
| `CreatedAt` | timestamptz | No | INDEX |
| `UpdatedAt` | timestamptz | Yes | |

#### `facility_issues`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `ReportedByMemberId`| uuid | No | FK -> `members.Id` (RESTRICT) |
| `EquipmentId`| uuid | Yes | FK -> `equipment.Id` (RESTRICT) |
| `LocationId` | uuid | No | FK -> `locations.Id` (RESTRICT) |
| `Title` | varchar(200) | No | |
| `Description`| varchar(2000)| No | Detailed issue report |
| `Severity` | integer | No | Enum (`Low=1`, `Medium=2`, `High=3`, `Critical=4`), INDEX |
| `Status` | integer | No | Enum (`Reported=1`, `InReview=2`, `Diagnosing=3`, `RequiresApproval=4`, `InRepair=5`, `Resolved=6`) |
| `ReportedAt` | timestamptz | No | INDEX |
| `ResolvedAt` | timestamptz | Yes | |
| `CreatedAt` | timestamptz | No | |
| `UpdatedAt` | timestamptz | Yes | |

#### `issue_images`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `IssueId` | uuid | No | FK -> `facility_issues.Id` (CASCADE) |
| `ImageUrl` | varchar(1000)| No | Storage URI / path |
| `ThumbnailUrl`| varchar(1000)| Yes | |
| `UploadedAt` | timestamptz | No | |

#### `repair_orders`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `IssueId` | uuid | No | FK -> `facility_issues.Id` (RESTRICT) |
| `EquipmentId`| uuid | No | FK -> `equipment.Id` (RESTRICT) |
| `OrderNumber` | varchar(50) | No | UNIQUE INDEX |
| `EstimatedCost`| numeric(12,2)| No | CHECK (`EstimatedCost >= 0`) |
| `ActualCost` | numeric(12,2)| Yes | CHECK (`ActualCost IS NULL OR ActualCost >= 0`) |
| `Status` | integer | No | Enum (`Draft=1`, `PendingApproval=2`, `Approved=3`, `Ordered=4`, `InProgress=5`, `Completed=6`) |
| `TechnicianName`| text | Yes | |
| `SupplierId` | uuid | Yes | Vendor supplying parts |
| `CreatedAt` | timestamptz | No | INDEX |
| `UpdatedAt` | timestamptz | Yes | |

#### `repair_order_items`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `RepairOrderId`| uuid | No | FK -> `repair_orders.Id` (CASCADE) |
| `PartName` | varchar(150) | No | |
| `PartNumber` | text | Yes | OEM / SKU part code |
| `Quantity` | integer | No | CHECK (`Quantity > 0`) |
| `UnitCost` | numeric(10,2) | No | CHECK (`UnitCost >= 0`) |
| `TotalCost` | numeric(12,2) | No | CHECK (`TotalCost >= 0`) |

#### `approvals`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `RepairOrderId`| uuid | No | UNIQUE INDEX 1:1, FK -> `repair_orders.Id` (CASCADE) |
| `ApproverUserId`| uuid | No | FK -> `users.Id` (RESTRICT) |
| `Decision` | integer | No | Enum (`Approved=1`, `Rejected=2`, `Revised=3`), INDEX |
| `Comments` | text | Yes | Approver notes |
| `DecidedAt` | timestamptz | No | INDEX |
| `ApprovalThreshold`| numeric(12,2)| No | CHECK (`ApprovalThreshold >= 0`) |
| `EstimatedCost`| numeric(12,2)| No | CHECK (`EstimatedCost >= 0`) |

---

### 3.6 AI Workflow State Domain (4 Tables)

#### `ai_workflows`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `IssueId` | uuid | No | UNIQUE INDEX 1:1, FK -> `facility_issues.Id` (CASCADE) |
| `WorkflowType` | varchar(100) | No | e.g. "FacilityIssueDiagnosis", INDEX |
| `Status` | integer | No | Enum (`Initiated=1`, `Planning=2`, `Validating=3`, `Analyzing=4`, `AwaitingApproval=5`, `Executing=6`, `Completed=7`, `Failed=8`), INDEX |
| `CurrentStep` | varchar(100) | No | e.g. "CostEstimation" |
| `DiagnosisSummary`| varchar(3000)| Yes| Structured diagnostic outcome |
| `RecommendedAction`| varchar(3000)| Yes| Remediation procedure |
| `EstimatedConfidenceScore`| double precision| No| e.g. 0.94 |
| `RequiresHumanApproval`| boolean | No | Default `true` |
| `HumanApprovalGranted`| boolean | Yes | Manager decision |
| `TotalTokensUsed`| integer | No | LLM usage metric |
| `ModelIdentifier`| varchar(100) | No | e.g. "gemini-1.5-pro" |
| `StructuredOutputPayloadJson`| jsonb | Yes | Strict JSON schema of recommendation |
| `StartedAt` | timestamptz | No | |
| `CompletedAt`| timestamptz | Yes | |
| `CreatedAt` | timestamptz | No | INDEX |
| `UpdatedAt` | timestamptz | Yes | |

#### `ai_workflow_steps`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `WorkflowId` | uuid | No | FK -> `ai_workflows.Id` (CASCADE) |
| `StepName` | varchar(100) | No | e.g. "CostAndPartAnalysis" |
| `StepOrder` | integer | No | COMPOSITE INDEX with WorkflowId |
| `Status` | varchar(50) | No | e.g. "Completed", "Failed" |
| `Summary` | varchar(2000)| Yes | Step outcome summary |
| `ExecutionDurationMs`| bigint | No | Telemetry latency |
| `ExecutedAt` | timestamptz | No | INDEX |

#### `ai_tool_executions`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `WorkflowStepId`| uuid | No | FK -> `ai_workflow_steps.Id` (CASCADE) |
| `ToolName` | varchar(100) | No | e.g. "EstimateRepairCost", INDEX |
| `InputParametersJson`| jsonb | No | Structured tool inputs (scrubbed of secrets) |
| `OutputResultJson` | jsonb | No | Structured tool return payload |
| `IsSuccess` | boolean | No | Tool execution status |
| `ExecutionTimeMs` | bigint | No | Tool invocation latency |
| `ExecutedAt` | timestamptz | No | INDEX |

#### `ai_validation_results`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `WorkflowId` | uuid | No | FK -> `ai_workflows.Id` (CASCADE) |
| `RuleName` | varchar(150) | No | e.g. "BudgetComplianceCheck" |
| `Passed` | boolean | No | INDEX |
| `ValidationMessage`| varchar(1000)| Yes | Validation justification |
| `EvaluatedAt`| timestamptz | No | |

---

### 3.7 Supporting Domain (2 Tables)

#### `audit_logs`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `EntityName` | varchar(100) | No | COMPOSITE INDEX with EntityId |
| `EntityId` | varchar(100) | No | Target entity identifier |
| `Action` | varchar(50) | No | e.g. "STOCK_ADJUSTMENT", "MEMBERSHIP_RENEWAL", INDEX |
| `UserId` | uuid | Yes | FK -> `users.Id` (SET NULL) |
| `OldValuesJson`| jsonb | Yes | Pre-mutation state |
| `NewValuesJson`| jsonb | Yes | Post-mutation state |
| `IpAddress` | varchar(45) | Yes | IPv4 / IPv6 client address |
| `UserAgent` | varchar(500) | Yes | Client browser / device |
| `Timestamp` | timestamptz | No | INDEX |

#### `notifications`
| Column | Type | Nullable | Constraints / Notes |
| :--- | :--- | :--- | :--- |
| `Id` | uuid | No | PRIMARY KEY |
| `UserId` | uuid | No | FK -> `users.Id` (CASCADE) |
| `Type` | integer | No | Enum (`General=1`, `Booking=2`, `Maintenance=3`, `Approval=4`) |
| `Title` | varchar(200) | No | |
| `Message` | varchar(1000)| No | |
| `IsRead` | boolean | No | Default `false`, COMPOSITE INDEX with UserId |
| `ReadAt` | timestamptz | Yes | |
| `TargetUrl` | varchar(500) | Yes | Deep-link for web / mobile navigation |
| `CreatedAt` | timestamptz | No | INDEX |
