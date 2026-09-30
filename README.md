# SMARTGYM: Enterprise Gym Management Ecosystem with Multi-Agent Agentic AI

An integrated, enterprise-grade, polyglot software engineering ecosystem built according to the **SmartGym Master Specification** and the **SE3090 Academic Baseline**. SmartGym unifies gym operations, supplement inventory, class scheduling, membership lifecycle management, and a deterministic 4-agent LangGraph AI workflow for high-impact facility maintenance.

---

## 1. Project Overview & Business Problem

Modern fitness facilities suffer from operational fragmentation across disconnected tools:
1. **Uncoordinated Inventory & Procurement**: Manual tracking leads to frequent stock-outs of high-demand supplements and delayed reorders.
2. **Delayed Equipment Maintenance**: Equipment breakdowns reported casually or verbally result in prolonged machine downtime, safety risks, and lost member satisfaction.
3. **Unstructured Escalations & Financial Risk**: Repair estimates and vendor communications lack automated safety vetting, structured diagnostic workflows, and financial authorization controls.
4. **Disjointed Member Experience**: Members lack real-time visibility into class capacities, membership renewals, and reported equipment repair timelines.

**SmartGym** addresses these challenges by delivering an authoritative, multi-tier system with:
- **Central Authority**: Single ASP.NET Core 8 Web API gateway with PostgreSQL 16 persistence.
- **Administrative Portal**: React 18 Web Admin Console with Human-in-the-Loop (HITL) approval interfaces.
- **Member & Trainer Experience**: Flutter 3 cross-platform mobile client with biometric-ready secure storage and camera integration.
- **Agentic AI Engine**: Python 3.13 FastAPI microservice orchestrating 4 distinct LangGraph agents with deterministic safety policies and audit logging.

---

## 2. Project Scope & Objectives

### Scope
- **Domain Coverage**: Full operational lifecycle across 4 core business components:
  1. Supplier & Supplement Inventory Management
  2. Feedback & Facility Issue Resolution (with Multi-Agent AI Core)
  3. Class Scheduling, Booking & Attendance Tracking
  4. Membership Subscription & Goal Management
- **Platforms**: Responsive Web Dashboard (Admin/Manager), Native Mobile Client (Android/iOS), Containerized Microservices.

### Objectives
- **Zero Direct Client-to-DB Connections**: Strict 3-tier / 4-tier architectural boundary enforcement.
- **Deterministic AI Safety**: 100% of LLM operations validated by schema guards, prompt injection defenses, and financial approval gates.
- **Sub-Millisecond Query Response**: Highly indexed relational schema achieving <1ms average query latency.
- **Comprehensive Quality Assurance**: 254 automated test assertions across Backend (103), React (40), Flutter (33), and AI (78).

---

## 3. User Roles & Permission Hierarchy

| Role | Core Responsibilities | Channels | Authorization Enforcement |
| :--- | :--- | :--- | :--- |
| **MEMBER** | Browses classes, books workouts, logs fitness goals, reports broken equipment with photos, tracks repair timelines. | Flutter Mobile Client | Role: `Member` |
| **TRAINER** | Views assigned classes and daily rosters, marks attendee check-ins, tracks member attendance metrics. | Flutter Mobile Client & Web Portal | Role: `Trainer` |
| **MANAGER** | Oversees facility tickets, reviews AI diagnostic proposals, approves/rejects repair orders exceeding financial threshold. | React Web Admin | Roles: `Manager`, `Admin` |
| **ADMIN** | Full system control: manages user roles, inventory restock, supplier orders, system audit logs, and global settings. | React Web Admin | Role: `Admin` |

---

## 4. Functional Requirements by Component

### Component 1: Supplier & Supplement Inventory Management
- **Inventory CRUD**: Manage products, stock quantities, reorder thresholds, and bin locations.
- **Transactional Stock Adjustments**: Atomic restock and manual adjustments with automatic audit trail logging.
- **Automated Low-Stock Alerts**: Proactive identification of inventory falling below reorder levels.
- **Supplier Directory**: Supplier contact details, category associations, and purchase order tracking.

### Component 2: Feedback & Facility Issue Resolution (Agentic AI Core)
- **Issue Submission**: Multi-part photo capture, severity classification, and equipment association.
- **4-Agent Multi-Agent Workflow**: Automated safety triage, planning, domain analysis, and action formulation.
- **Human-in-the-Loop Financial Gate**: Mandatory approval for repairs exceeding the threshold (LKR 10,000 / LKR 25,000).
- **Audit & Timeline Updates**: Real-time stage progression (`Submitted` → `Evaluating` → `Pending Approval` → `Approved` → `Vendor Contacted` → `Resolved`).

### Component 3: Class Scheduling & Attendance Booking
- **Timetable Management**: Fitness class definitions, category tagging, trainer assignments, and room schedules.
- **Booking Engine**: Capacity enforcement, waitlisting prevention, and atomic booking transactions.
- **Trainer Attendance Verification**: Check-in confirmation with instant attendance record persistence.

### Component 4: Membership & Goal Tracking
- **Tiered Memberships**: Bronze, Silver, Gold, Platinum plans with duration and benefit configurations.
- **Lifecycle Management**: Enrollment, renewal, cancellation, and automated expiry calculation.
- **Goal & Metric Logs**: Target weight, body fat %, muscle mass, and historical milestone tracking.

---

## 5. Technology Stack & Justification

```
┌────────────────────────────────────────────────────────────────────────┐
│                        SMARTGYM TECHNOLOGY STACK                       │
├──────────────────────┬─────────────────────────┬───────────────────────┤
│ Layer                │ Technology              │ Rationale             │
├──────────────────────┼─────────────────────────┼───────────────────────┤
│ Backend API          │ ASP.NET Core 8 (C#)     │ Strict type safety,   │
│                      │                         │ high concurrency,     │
│                      │                         │ built-in DI & AuthN   │
├──────────────────────┼─────────────────────────┼───────────────────────┤
│ Database             │ PostgreSQL 16 + EF Core │ Strong ACID guarantees│
│                      │                         │ enterprise indexing,  │
│                      │                         │ Code-First migrations │
├──────────────────────┼─────────────────────────┼───────────────────────┤
│ Frontend Web Admin   │ React 18 + Vite         │ Component isolation,  │
│                      │ Redux Toolkit           │ SPA routing, robust   │
│                      │                         │ testing ecosystem     │
├──────────────────────┼─────────────────────────┼───────────────────────┤
│ Mobile App           │ Flutter 3.24 + Dart     │ Native cross-platform,│
│                      │ Riverpod + SecureStorage│ hardware camera, 60fps│
├──────────────────────┼─────────────────────────┼───────────────────────┤
│ Agentic AI Service   │ Python 3.13 + FastAPI   │ StateGraph workflow,  │
│                      │ LangGraph + Pydantic v2 │ conditional routing,  │
│                      │                         │ structured output     │
├──────────────────────┼─────────────────────────┼───────────────────────┤
│ CI/CD & Containers   │ GitHub Actions + Docker │ Automated multi-stage │
│                      │ Compose                 │ builds and zero-drift │
└──────────────────────┴─────────────────────────┴───────────────────────┘
```

---

## 6. Architecture & Data Flow

```
[Flutter Mobile App] ───┐
  (Member / Trainer)    │ HTTPS (JWT)
                        ▼
            [ASP.NET Core 8 Web API] ◄───► [PostgreSQL 16 Database]
            (Central Auth & Gate)          (36 Relational Tables)
                        ▲
                        │ HTTPS (JWT)
[React 18 Web Admin] ───┘
   (Admin / Manager)
                        │
                        ▼ REST / Internal Private HTTP
            [AI Microservice (FastAPI)]
            ┌──────────────────────────────────────────────┐
            │ LangGraph State Machine (4 Distinct Agents):  │
            │  1. Safety & Content Validation Agent        │
            │  2. Planner Agent (Coordinator)              │
            │  3. Gym Domain Analysis Agent                │
            │  4. Action & Tool Execution Agent            │
            └──────────────────────────────────────────────┘
                        │
                        ▼ Transactional Dispatch
            [Third-Party Vendor Email Adapter]
```

### Architectural Rules
1. **Single Authoritative Backend**: All database operations and external actions must be authorized and routed through the ASP.NET Core API.
2. **Client Isolation**: React and Flutter NEVER connect directly to PostgreSQL, Redis, or internal AI microservices.
3. **Fail-Safe AI Execution**: Unhandled AI exceptions gracefully revert to safe human escalation tickets without terminating database transactions.

---

## 7. Database Architecture (PostgreSQL 16 & EF Core)

The datastore contains **36 relational tables** with foreign key constraints, unique indexes, and audit columns:
- **Identity & Access**: `users`, `roles`, `user_roles`, `refresh_tokens`.
- **Members & Subscriptions**: `members`, `memberships`, `membership_plans`, `goals`, `progress_records`.
- **Fitness Operations**: `locations`, `equipment`, `class_categories`, `fitness_classes`, `class_schedules`, `bookings`, `attendances`.
- **Supplements & Inventory**: `product_categories`, `products`, `inventory_items`, `stock_movements`, `suppliers`, `purchase_orders`, `purchase_order_items`.
- **Facility Maintenance**: `facility_issues`, `issue_images`, `repair_orders`, `repair_order_items`.
- **Feedback & Communication**: `feedbacks`, `notifications`, `audit_logs`.
- **Agentic AI & Approvals**: `ai_workflows`, `ai_workflow_steps`, `ai_validation_results`, `ai_tool_executions`, `approvals`.

---

## 8. Frontend Web Application (React 18 + Vite)

Located in `frontend-web/smartgym-web`:
- **State Management**: Redux Toolkit slices (`authSlice`, `facilitySlice`, `inventorySlice`, `approvalSlice`).
- **Routing**: `react-router-dom` v6 with protected routes and role navigation guards.
- **Key Modules**:
  - `ApprovalsPage`: Human-in-the-Loop review queue displaying AI diagnosis, estimated repair costs, and one-click authorization.
  - `FacilityResolutionPage`: Complete issue registry with real-time status badges and timeline drill-down.
  - `InventoryPage`: Real-time stock levels, low-stock threshold alerts, and restocking modal.
  - `ClassSchedulePage`: Timetable builder and class registration grid.
  - `MembershipsPage` & `MembersPage`: Subscription lifecycle and member profiles.

---

## 9. Mobile Application (Flutter 3.24)

Located in `mobile/smartgym_mobile`:
- **State Architecture**: `flutter_riverpod` providers for declarative dependency injection and responsive state updates.
- **Security**: Hardware-backed token encryption via `flutter_secure_storage` (Android Keystore / iOS Keychain).
- **Device Features**: `image_picker` for photographing broken gym equipment.
- **Key Screens**:
  - `FacilityReportScreen`: Interactive form with photo attachment and live severity tagging.
  - `FacilityIssueDetailScreen`: Chronological resolution timeline tracking the 4-agent AI lifecycle.
  - `ClassesScreen`: Real-time booking slots with remaining capacity counters.
  - `TrainerDashboardScreen`: Class roster and one-tap attendance check-in.
  - `MembershipScreen`: Active subscription card and renewal modal.

---

## 10. Agentic AI Multi-Agent System (LangGraph)

Located in `ai-service/smartgym_ai`:
1. **Safety Validation Agent**:
   - Sandboxes user input against prompt injection patterns (`IGNORE PREVIOUS INSTRUCTIONS`).
   - Verifies ticket completeness and business rule compliance.
2. **Planner Agent**:
   - Decomposes the maintenance goal into sequenced phases with explicit agent assignments.
3. **Gym Domain Analysis Agent**:
   - Invokes allow-listed tools: `getEquipmentDetails`, `checkInventory`, `getSupplierDetails`.
   - Formulates mechanical root-cause hypotheses, part numbers, and cost estimates.
4. **Action Execution Agent**:
   - Evaluates Human-in-the-Loop approval status.
   - Dispatches `createRepairOrder`, `sendVendorEmail`, and `updateFacilityIssueStatus`.
   - Enforces unique idempotency keys (`vendor-action-{issue_id}-{action_type}`).

---

## 11. Third-Party Integration & Email Dispatch

- **Transactional Adapter**: `SendVendorEmailTool` dispatches RFQ emails to certified suppliers upon approval.
- **Fault-Tolerant Degraded Mode**: If the third-party email provider experiences a network timeout or 5xx outage, the repair order remains safely committed in PostgreSQL, and the workflow records a degraded status with an alert in `audit_logs`.

---

## 12. Security Audit Summary

- **JWT Authentication**: HMAC-SHA256 with 15-minute access tokens and 7-day refresh token rotation.
- **Password Hashing**: Salted PBKDF2 (SHA-256) with 10,000 iterations via `IPasswordHasher<User>`.
- **SQL Safety**: 100% parameterized queries via Entity Framework Core and Npgsql; zero raw SQL string concatenation.
- **CORS Whitelisting**: Restricted explicitly to `http://localhost:3000` and `http://localhost:5173`.
- **Approval Bypass Prevention**: Action tools deterministically check `approval_status == 'APPROVED'` and verify the manager's role in the database before mutating state.
- **File Upload Security**: Strict MIME-type whitelisting (`image/jpeg`, `image/png`, `image/webp`), 5 MB size cap, and random UUID storage isolation.

---

## 13. Testing & Empirical Quality Assurance

Full automated test suite with **254 passing test assertions**:
- **Backend Tests (103/103 passed)**: `dotnet test tests/backend/SmartGym.Api.Tests`
  - Unit, Service, DTO validation, AuthN/AuthZ, Controller, DB integration, Atomic transactions, Security audit.
- **Frontend Web Tests (40/40 passed across 13 files)**: `npm test` in `frontend-web/smartgym-web`
  - Vitest + Testing Library: Component rendering, form validation, role routes, approvals queue.
- **Mobile Tests (33/33 passed)**: `flutter test` in `mobile/smartgym_mobile`
  - Unit, widget, form validation, navigation, image picker mocking, API integration.
- **AI Microservice Tests (78/78 passed)**: `pytest tests/ai`
  - All 8 Golden Cases, schema validators, agent delegation, prompt injection defenses, fault tolerance.

---

## 14. Empirical Performance Benchmarks (Real Measurements)

Measurements executed against live local environment:
- **PostgreSQL Database**:
  - Sample Size: 50 real queries
  - **Mean Query Latency**: **0.304 ms**
  - **P50 Latency**: **0.111 ms**
  - **P95 Latency**: **1.523 ms**
  - **Success Rate**: **100.0%**
- **ASP.NET Core Web API**:
  - Concurrent Health Requests: 50 concurrent requests, **100.0% success rate**, Mean latency 2.3 ms.
  - Authenticated Paginated Query: 25 concurrent requests, **100.0% success rate**, P50 337 ms, P95 372 ms.
- **AI Multi-Agent Pipeline**:
  - Workflows Executed: 24 full multi-agent lifecycles
  - Concurrency: 8 parallel worker threads
  - **Mean Workflow Latency**: **8,032.88 ms**
  - **P50 Latency**: **7,805.86 ms**
  - **Success Rate**: **100.0%** (24/24 completed)

---

## 15. Continuous Integration (GitHub Actions)

Configured in `.github/workflows/ci.yml` triggering on push and PR to `main`:
1. `backend-ci`: Restore, Release build, and test execution on .NET 8.
2. `ai-service-ci`: Python 3.12 setup, requirements install, and `pytest tests/ai`.
3. `frontend-ci`: Node 20 setup, `npm test`, and `npm run build`.
4. `flutter-ci`: Flutter 3.24 setup, `flutter pub get`, and `flutter test`.
5. `docker-build-ci`: Docker Buildx container verification for API, Web, and AI.

---

## 16. Installation & Local Setup

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+ & npm](https://nodejs.org/)
- [Python 3.11+](https://www.python.org/)
- [Flutter 3.24+ SDK](https://flutter.dev/)
- [PostgreSQL 16+](https://www.postgresql.org/)

### Quick Start via Docker Compose
```bash
# 1. Clone repository and setup environment
git clone https://github.com/IT24103194/SEF-project.git
cd SEF-project
cp .env.example .env

# 2. Build and launch all containers
docker compose up -d --build

# Endpoints:
# - Web Admin:      http://localhost:3000
# - Backend API:    http://localhost:5000/swagger
# - AI Service:     http://localhost:8000/docs
# - PostgreSQL:     localhost:5432
```

### Manual Host Execution
```bash
# 1. Start PostgreSQL (Port 5432, Database: smartgym)

# 2. Start ASP.NET Core API
cd backend/SmartGym.Api
dotnet restore
dotnet run --configuration Release

# 3. Start AI Microservice
cd ai-service
pip install -r requirements.txt
uvicorn smartgym_ai.main:app --host 0.0.0.0 --port 8000 --reload

# 4. Start React Web Admin
cd frontend-web/smartgym-web
npm install
npm run dev

# 5. Start Flutter Mobile App
cd mobile/smartgym_mobile
flutter pub get
flutter run
```

---

## 17. Environment Variables Reference

| Variable | Target Component | Default / Example Value | Description |
| :--- | :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | Backend API | `Host=localhost;Port=5432;Database=smartgym;Username=postgres;Password=1234` | PostgreSQL connection string |
| `JwtSettings__Secret` | Backend API | `SuperSecretSmartGym256BitSecurityKey2026!` | 256-bit HMAC key |
| `AiService__BaseUrl` | Backend API | `http://localhost:8000` | Python AI service endpoint |
| `DATABASE_URL` | AI Service | `postgresql://postgres:1234@localhost:5432/smartgym` | AI direct DB tool connection |
| `AI_APPROVAL_COST_THRESHOLD` | AI Service | `10000.0` | Cost triggering approval (LKR) |
| `LLM_PROVIDER` | AI Service | `mock` (or `openai`, `anthropic`, `gemini`) | Active LLM client |
| `VITE_API_URL` | Frontend Web | `http://localhost:5000/api` | API gateway URL |
| `API_BASE_URL` | Mobile App | `http://10.0.2.2:5000/api` | Emulator API gateway URL |

---

## 18. Troubleshooting Common Issues

1. **PostgreSQL Connection Refused (`5432`)**:
   - Ensure the PostgreSQL service is running: `Get-Service postgresql*` (Windows) or `sudo systemctl status postgresql` (Linux).
   - Verify username/password in `appsettings.json` matches local PostgreSQL instance.
2. **CORS Blocked in Browser**:
   - Verify `Cors__AllowedOrigins` in `appsettings.json` includes `http://localhost:3000` and `http://localhost:5173`.
3. **AI Microservice ModuleNotFoundError (`smartgym_ai`)**:
   - Run pytest using `pytest` (with root `pytest.ini`) or set `PYTHONPATH=ai-service`.
4. **Flutter Android Emulator Network Timeout (`10.0.2.2`)**:
   - When running on an Android emulator, `localhost` refers to the device itself. Ensure the mobile app uses `http://10.0.2.2:5000/api` to connect to the host machine.
