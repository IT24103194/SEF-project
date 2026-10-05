# SmartGym Academic Individual Student Reports (SE3090)

**Module**: SE3090 — Software Engineering Frameworks  
**Academic Institution**: Sri Lanka Institute of Information Technology (SLIIT)  
**Project**: SmartGym — Integrated Gym & Facility Management System with Agentic AI  
**Academic Year**: 2026  

---

## Student 1: Lead Architect & Component 1 Owner
- **Student Name**: IT24103194 (Group Lead)
- **Student Registration ID**: `IT24103194`
- **Email**: `it24103194@my.sliit.lk`

### 1. Contribution Overview
Served as technical team lead, architecting the 4-tier monorepo structure, setting up the Docker Compose environment, configuring the GitHub Actions CI/CD pipeline, and taking end-to-end full-stack ownership of **Component 1: Supplier & Supplement Inventory Management** and the **Gym Domain Analysis Agent**.

### 2. Owned Component
**Component 1: Supplier & Supplement Inventory Management**
- Complete lifecycle of supplement products, inventory levels, reorder thresholds, supplier orders, and atomic stock movements.

### 3. Backend Implementation (ASP.NET Core 8)
- Authored `InventoryController.cs`, `ProductsController.cs`, `SuppliersController.cs`.
- Implemented `InventoryService.cs` with atomic transactional stock adjustments and automated low-stock query optimization.
- Created FluentValidation validators (`InventoryAdjustmentRequestValidator`, `ProductCreateRequestValidator`).
- Configured RFC 7807 ProblemDetails middleware and Swagger OpenAPI documentation.

### 4. Database Implementation (PostgreSQL 16 & EF Core)
- Designed tables: `products`, `product_categories`, `inventory_items`, `stock_movements`, `suppliers`, `purchase_orders`, `purchase_order_items`.
- Configured unique SKU indexes, foreign key restrict constraints, and non-clustered performance indexes on `QuantityInStock`.
- Managed initial EF Core migration and seed data in `SmartGymDbContext.cs`.

### 5. Frontend Web Implementation (React 18 & Vite)
- Built `InventoryPage.jsx`: Real-time inventory table, low-stock threshold warning indicators, and restocking modal.
- Created Redux `inventorySlice.js` with asynchronous thunks for fetching inventory and dispatching stock adjustments.
- Designed reusable UI components: `StockBadge.jsx`, `Modal.jsx`, `AlertBanner.jsx`.

### 6. Mobile Implementation (Flutter 3.24)
- Built `InventoryScreen.dart`: Mobile inventory catalog displaying supplement stock levels with visual low-stock highlights.
- Created `StockAdjustmentBottomSheet.dart`: Modal form allowing authorized staff to record stock movements on-the-go.
- Configured Riverpod `inventoryProvider.dart`.

### 7. Agentic AI Contribution
- Implemented the **Gym Domain Analysis Agent** (`domain_analysis_agent.py`):
  - Engineered tools: `getEquipmentDetails`, `checkInventory`, `getSupplierDetails`.
  - Implemented root-cause hypothesis generation and spare part pricing estimation logic.
  - Linked agent outputs to Pydantic v2 schemas (`DomainAnalysisOutput`).

### 8. Testing & Quality Assurance
- Backend: Authored `InventoryApiTests.cs`, `ProductApiTests.cs`, and `TransactionAndSecurityAuditTests.cs` (atomic stock restock test).
- React: Authored `InventoryPage.test.jsx` (4 passing tests).
- Flutter: Authored `inventory_screen_test.dart` (5 passing tests).
- AI: Authored `test_domain_analysis_agent.py` (10 passing tests) and Golden Case 1 multi-agent tests.

### 9. Git & Version Control Evidence
- **Branch**: `IT24103194`
- **Key Commits**:
  - `8205e4b`: `feat(phase-17): SmartGym Full QA, Security Audit, CI/CD, Performance and Deployment`
  - `c68b39a`: `feat(phase-16): complete smartgym agentic ai workflow and cross-platform integration`
  - `94b1fff`: `feat(phase-14): implement gym domain analysis agent with data retrieval tools and persistence`
  - `c67e817`: `feat(phase-10): implement supplier and inventory management backend APIs`

### 10. Pull Request Evidence
- **PR #1**: Monorepo Architecture & Backend Layered Core setup.
- **PR #4**: Supplier & Inventory Domain Services with Atomic Stock Movements.
- **PR #7**: LangGraph Gym Domain Analysis Agent integration.
- **PR #10**: Phase 17 Full System QA, Security Hardening, and Benchmark suite.

### 11. Challenges Encountered & Resolutions
- *Challenge*: Handling concurrent stock adjustment race conditions.
  - *Resolution*: Implemented database row-level locking via EF Core execution strategies and atomic transactions.
- *Challenge*: Npgsql snake_case mapping discrepancies between C# PascalCase and PostgreSQL tables.
  - *Resolution*: Configured `UseSnakeCaseNamingConvention()` in `SmartGymDbContext` and added quoted column mappings.

### 12. Key Learning
- Mastering asynchronous state graph orchestration in LangGraph.
- Enterprise API security patterns (HMAC-SHA256 JWT, refresh token rotation, RFC 7807 error structures).
- Performance tuning with indexed PostgreSQL relational schemas.

### 13. AI Usage Log Summary
- Used Google Antigravity / Gemini for scaffolding repetitive DTOs and generating initial test boilerplate.
- Every generated snippet was reviewed, manually refactored, and verified with unit tests. Zero unverified code merged.

### 14. Reflection on AI in Software Engineering
Generative AI significantly accelerates boilerplate drafting and regression test generation. However, high-stakes enterprise systems require deterministic human verification; automated tools cannot replace critical thinking regarding financial gates, atomic transaction boundaries, and security constraints.

### 15. Student Declaration
I hereby certify that this report and the software contributions documented herein represent my authentic individual work carried out in accordance with academic integrity guidelines.

**Signature**: *IT24103194*  
**Date**: October 1, 2026  

---

## Student 2: Component 2 Owner & AI Safety Specialist
- **Student Name**: Pulitha (Component 2 Lead)
- **Student Registration ID**: `IT24103362`
- **Email**: `IT24103362@my.sliit.lk`
- **Branch**: `IT24103362`

### 1. Contribution Overview
Responsible for the full-stack implementation of the member feedback and facility issue reporting system, along with the **Safety & Content Validation Agent** and the **Human-in-the-Loop Approval Queue**.

### 2. Owned Component
**Component 2: Feedback & Facility Resolution**
- Equipment issue reporting with photo evidence, resolution status tracking, member ratings, and manager moderation.

### 3. Backend Implementation (ASP.NET Core 8)
- Authored `FacilityIssuesController.cs`, `FeedbackController.cs`, `ApprovalsController.cs`.
- Implemented `FacilityIssueService.cs` handling multipart image uploads, status transitions, SLA calculations, and AI workflow invocation.
- Developed `ApprovalService.cs` enforcing Human-in-the-Loop decision logic and RBAC manager authorization.

### 4. Database Implementation (PostgreSQL 16 & EF Core)
- Designed tables: `facility_issues`, `issue_images`, `feedbacks`, `approvals`, `ai_validation_results`.
- Configured foreign key cascade deletion for images and restrict deletion for resolving managers.
- Added indexes on `Severity`, `Status`, and `ReportedAt`.

### 5. Frontend Web Implementation (React 18 & Vite)
- Built `ApprovalsPage.jsx`: The central HITL review queue where managers review AI repair proposals and click Approve/Reject.
- Built `FacilityResolutionPage.jsx`: Real-time issue table with status badge color coding and severity filters.
- Implemented Redux `approvalSlice.js` and `facilitySlice.js`.

### 6. Mobile Implementation (Flutter 3.24)
- Built `FacilityReportScreen.dart`: Multi-part issue submission form with camera integration (`image_picker`).
- Built `FacilityIssueDetailScreen.dart`: Resolution timeline showing real-time progress from submission to vendor dispatch.
- Built `FeedbackScreen.dart`: Interactive star rating and comment submission form.

### 7. Agentic AI Contribution
- Implemented the **Safety & Content Validation Agent** (`safety_validation_agent.py`):
  - Designed prompt injection defense heuristic scanner (blocking adversarial tokens like `IGNORE ALL INSTRUCTIONS`).
  - Implemented ticket completeness and gym business rule validation.
  - Authored `_safe_failure_node` in LangGraph for graceful error degradation.

### 8. Testing & Quality Assurance
- Backend: Authored `FacilityIssuesApiTests.cs`, `ApprovalsApiTests.cs`, `FeedbackApiTests.cs`.
- React: Authored `ApprovalsPage.test.jsx`, `FacilityResolutionPage.test.jsx`.
- Flutter: Authored `facility_screen_test.dart`, `feedback_screen_test.dart`.
- AI: Authored `test_safety_agent.py`, `test_safe_failure.py`, and Golden Cases 2, 3, 4, 5.

### 9. Git & Version Control Evidence
- **Branch**: `IT24103362`
- **Component Commits**: Iterative commits covering Facility reporting, SLA calculation, Safety validation agent, and HITL approvals.

### 10. Challenges & Learning
- *Challenge*: Preventing approval bypass attempts where unapproved tools might execute.
  - *Resolution*: Enforced hard checks in both ASP.NET Core `ApprovalService` and Python `ActionExecutionAgent` verifying database approval status.

### 11. Student Declaration
I certify that the contributions described above represent my individual work in collaboration with the SmartGym project team.

**Signature**: *IT24103362 (Pulitha)*  
**Date**: October 6, 2026  

---

## Student 3: Component 3 Owner & AI Coordinator Specialist
- **Role**: Backend & Mobile Engineer, AI Planning Specialist
- **Assigned Component**: **Component 3: Class Scheduling & Attendance Booking**

### 1. Contribution Overview
Responsible for full-stack fitness class scheduling, timetable management, member booking with capacity enforcement, trainer attendance rosters, and the **Planner Coordinator Agent**.

### 2. Owned Component
**Component 3: Class Scheduling & Attendance Booking**
- Class definitions, categories, weekly timetable schedules, atomic booking reservations, and attendance tracking.

### 3. Backend Implementation (ASP.NET Core 8)
- Authored `FitnessClassesController.cs`, `ClassCategoriesController.cs`, `ClassSchedulesController.cs`, `BookingsController.cs`, `AttendancesController.cs`.
- Implemented atomic booking reservation in `BookingService.cs` preventing over-capacity reservations via database transactions.

### 4. Database Implementation (PostgreSQL 16 & EF Core)
- Designed tables: `class_categories`, `fitness_classes`, `class_schedules`, `bookings`, `attendances`, `locations`.
- Configured check constraints for maximum room capacity and unique index on `(ClassScheduleId, MemberId)` to prevent duplicate bookings.

### 5. Frontend Web Implementation (React 18 & Vite)
- Built `ClassSchedulePage.jsx`: Interactive timetable grid displaying scheduled classes, assigned trainers, and available seats.
- Integrated Redux slice for class schedules and category filters.

### 6. Mobile Implementation (Flutter 3.24)
- Built `ClassesScreen.dart`: Member booking interface showing live remaining spots and booking confirmation dialogs.
- Built `TrainerDashboardScreen.dart`: Trainer daily roster view with one-tap attendance check-in buttons.

### 7. Agentic AI Contribution
- Implemented the **Planner Coordinator Agent** (`planner_agent.py`):
  - Designed hierarchical task decomposition logic breaking high-level facility issues into 4 discrete execution phases.
  - Configured typed Pydantic models (`PlannerInput`, `PlannerOutput`, `PlannedStep`).
  - Integrated conditional graph transitions in `workflow_engine.py`.

### 8. Testing & Quality Assurance
- Backend: Authored `ClassSchedulesApiTests.cs`, `BookingsApiTests.cs`, `AttendancesApiTests.cs`.
- React: Authored `ClassSchedulePage.test.jsx`.
- Flutter: Authored `classes_screen_test.dart`, `trainer_dashboard_test.dart`.
- AI: Authored `test_planner_agent.py` and Golden Case 1 planning verification.

### 9. Student Declaration
I certify that the contributions described above represent my authentic work on the SmartGym project.

**Date**: October 1, 2026  

---

## Student 4: Component 4 Owner & AI Tool Integration Specialist
- **Role**: Full-Stack Developer, AI Tool Integration Specialist
- **Assigned Component**: **Component 4: Membership & Goal Tracking**

### 1. Contribution Overview
Responsible for member subscription lifecycle management, fitness goal and progress milestone tracking, administrative KPI reporting, and the **Action & Tool Execution Agent**.

### 2. Owned Component
**Component 4: Membership & Goal Tracking**
- Membership plans, subscription activation/renewal/cancellation, fitness goal metrics, progress logging, and operational reports.

### 3. Backend Implementation (ASP.NET Core 8)
- Authored `MembershipsController.cs`, `MembershipPlansController.cs`, `GoalsController.cs`, `ProgressRecordsController.cs`, `ReportsController.cs`.
- Developed `MembershipService.cs` calculating pro-rated renewals, expiration date extensions, and automated status updates.
- Authored `ReportsService.cs` aggregating membership revenue and facility ticket statistics.

### 4. Database Implementation (PostgreSQL 16 & EF Core)
- Designed tables: `membership_plans`, `memberships`, `goals`, `progress_records`, `repair_orders`, `repair_order_items`, `ai_tool_executions`.
- Configured date validation constraints (`EndDate > StartDate`).

### 5. Frontend Web Implementation (React 18 & Vite)
- Built `MembershipsPage.jsx`: Subscription plan catalog and member subscription status dashboard.
- Built `ReportsPage.jsx`: Visual SVG KPI charts for revenue, active memberships, and facility downtime.
- Built `DashboardPage.jsx`: Executive management summary view.

### 6. Mobile Implementation (Flutter 3.24)
- Built `MembershipScreen.dart`: Active membership card with renewal bottom sheet modal.
- Built `GoalsScreen.dart` & `ProgressScreen.dart`: Metric cards for weight, body fat %, and milestone charts.

### 7. Agentic AI Contribution
- Implemented the **Action & Tool Execution Agent** (`action_execution_agent.py`):
  - Developed action tools: `createRepairOrder`, `sendVendorEmail`, `updateFacilityIssueStatus`, `createNotification`.
  - Implemented replay protection and idempotency key caching (`vendor-action-{issue_id}-{action_type}`).
  - Implemented resilient degraded mode for external email timeouts.

### 8. Testing & Quality Assurance
- Backend: Authored `MembershipsApiTests.cs`, `GoalsApiTests.cs`, `ReportsApiTests.cs`.
- React: Authored `MembershipsPage.test.jsx`, `ReportsPage.test.jsx`, `DashboardPage.test.jsx`.
- Flutter: Authored `membership_screen_test.dart`.
- AI: Authored `test_action_agent.py`, `test_timeout_and_retry.py`, and Golden Cases 7 & 8.

### 9. Student Declaration
I certify that the contributions described above represent my individual authentic work on the SmartGym project.

**Date**: October 1, 2026  
