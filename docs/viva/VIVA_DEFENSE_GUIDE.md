# SmartGym Comprehensive Viva Defense Guide & Model Q&A

**Prepared For**: SE3090 Viva Examination  
**Audience**: University Examiners, Technical Evaluators & Project Team  
**Scope**: Complete Full-Stack Architecture, Backend, Database, Frontend, Mobile, Multi-Agent AI, Security, Performance & DevOps.

---

## 1. ASP.NET Core 8 Web API & Architecture

### Q1.1: Why did you choose ASP.NET Core 8 instead of Node.js / Express or Django for the primary backend?
**Answer**:
ASP.NET Core 8 provides:
1. **High Concurrency & Throughput**: Native asynchronous I/O and compiled Kestrel web server deliver top-tier benchmark performance (as evidenced by our 50-request concurrent load test with 2.3ms average response time).
2. **Strict Type Safety & Enterprise Tooling**: C# strong typing prevents entire categories of runtime bugs across DTOs and database models.
3. **Built-in Dependency Injection & Security**: Native inversion of control, middleware pipelines, and declarative authorization policies (`[Authorize(Roles = "...")]`) without relying on fragmented third-party packages.

### Q1.2: How are Controllers, DTOs, and Services decoupled in your solution?
**Answer**:
We adhere to Clean Architecture:
- **Controllers** (`SmartGym.Api/Controllers`): Responsible solely for HTTP transport concerns, routing, model binding, and returning standard RFC 7807 ProblemDetails responses. They contain zero database logic.
- **DTOs** (`SmartGym.Api/DTOs`): Data Transfer Objects encapsulate request and response contracts, decoupled from database entities. They are validated via `FluentValidation`.
- **Services** (`SmartGym.Api/Services`): Contain pure business logic and transaction boundaries (e.g. `InventoryService`, `FacilityIssueService`, `BookingService`). They are registered as scoped services in `Program.cs` and injected into controllers.

### Q1.3: Explain how Dependency Injection (DI) is structured in `Program.cs`.
**Answer**:
Services are registered using appropriate service lifetimes:
- **Scoped**: `SmartGymDbContext` (per HTTP request), domain services (`IInventoryService`, `IFacilityIssueService`, `IAuthService`, `IApprovalService`), and repository layers.
- **Singleton**: Configuration wrappers (`JwtSettings`), caching layers, and thread-safe HTTP client factories (`IAiServiceClient`).
- **Transient**: Lightweight stateless utilities and validators (`IValidator<T>`).

---

## 2. EF Core 8 & PostgreSQL Database

### Q2.1: How do you prevent SQL injection in SmartGym?
**Answer**:
100% of database interactions execute through Entity Framework Core using parameterized queries via Npgsql. When LINQ queries (e.g. `db.FacilityIssues.Where(f => f.Title == search)`) are executed, parameters are passed as binary SQL parameter markers (`@p0`), never via string concatenation. In our automated test `TransactionAndSecurityAuditTests.SqlSafety_ParameterizedQueries_PreventSqlInjection`, passing `' OR 1=1 --` safely evaluates as a harmless literal string.

### Q2.2: How are relationships, foreign keys, and delete behaviors configured?
**Answer**:
Configured fluently in `SmartGymDbContext.OnModelCreating`:
- **Cascade Delete**: Used for tightly coupled child entities (e.g. `ai_workflows` → `ai_workflow_steps` → `ai_tool_executions`, `facility_issues` → `issue_images`). Deleting the parent automatically deletes the children.
- **Restrict / NoAction**: Used for referenced audit and actor entities (e.g. `facility_issues` → `users`, `approvals` → `users`). This prevents accidental deletion of user accounts from corrupting audit trails.

### Q2.3: What indexes were implemented and what query performance did they yield?
**Answer**:
- Unique indexes on `users.Email`, `products.Sku`, and composite `(ClassScheduleId, MemberId)` on bookings.
- Non-clustered performance indexes on foreign keys (`FacilityIssueId`, `EquipmentId`, `ClassScheduleId`) and frequently filtered status columns (`Status`, `Severity`, `QuantityInStock`).
- **Result**: Measured average query latency is **0.304 ms** across 50 iterations with P50 of **0.111 ms**.

---

## 3. Security, JWT & RBAC

### Q3.1: Walk through the complete JWT authentication and token refresh lifecycle.
**Answer**:
1. Member logs in via `/api/auth/login`.
2. Password is verified using PBKDF2 with SHA-256 (10,000 iterations).
3. If valid, `AuthService` issues:
   - A short-lived **Access Token** (15-minute lifespan, HMAC-SHA256, carrying `sub`, `email`, and `role` claims).
   - A cryptographically random **Refresh Token** (7-day lifespan, persisted in `refresh_tokens`).
4. When the access token expires, the client calls `/api/auth/refresh-token`.
5. The API validates the refresh token in PostgreSQL, revokes the old token, and issues a rotated pair.
6. The user can explicitly log out via `/api/auth/revoke-token`.

### Q3.2: How is Role-Based Access Control (RBAC) enforced? Can a Member bypass it?
**Answer**:
RBAC is enforced via ASP.NET Core `[Authorize(Roles = "...")]` attributes on controllers and policy checks in business services:
- `/api/approvals` endpoints require `Roles = "Admin,Manager"`.
- If a user with role `Member` presents a valid JWT, the ASP.NET Core `JwtBearerHandler` decodes the role claim, detects a role mismatch, and immediately short-circuits the pipeline with `403 Forbidden`. The controller action is never invoked.
- This is proven in automated test `BE-RBAC-01`.

---

## 4. Frontend Web (React 18)

### Q4.1: Why did you choose Redux Toolkit for state management?
**Answer**:
Refer to `ADR-001`. Redux Toolkit provides predictable, unidirectional state mutations via Immer, built-in asynchronous thunks (`createAsyncThunk`) for backend communication, and isolated testability. It allows distinct slices (`approvalSlice`, `facilitySlice`, `inventorySlice`) to be tested in isolation using Vitest.

### Q4.2: How do protected routes and role navigation work?
**Answer**:
`AppRoutes.jsx` implements `<ProtectedRoute allowedRoles={['Admin', 'Manager']}>`. It reads the authenticated user's role from `authSlice`. If unauthenticated, it redirects to `/login`. If the role is unauthorized, it redirects to `/dashboard` or an unauthorized access notice.

---

## 5. Mobile Application (Flutter 3.24)

### Q5.1: Why Riverpod instead of Provider or BLoC?
**Answer**:
Refer to `ADR-002`. Riverpod provides compile-time safety (eliminating `ProviderNotFoundException`), avoids global mutable state, and allows clean mocking in widget tests via `ProviderScope(overrides: [...])` without the heavy boilerplate of BLoC.

### Q5.2: How are security credentials stored on the mobile device?
**Answer**:
Tokens are never stored in plaintext `SharedPreferences`. We use `flutter_secure_storage`, which leverages **Android Keystore** (AES encryption) and **iOS Keychain** (hardware Secure Enclave).

### Q5.3: How does the camera integration work for facility reporting?
**Answer**:
We use the `image_picker` package in `FacilityReportScreen.dart`. Members tap "Capture Photo", which triggers the native camera intent. The photo is loaded into memory, converted to multipart form data, and sent via POST to `/api/facility-issues`.

---

## 6. Multi-Agent Agentic AI & LangGraph

### Q6.1: Name the 4 AI agents and explain their specific responsibilities.
**Answer**:
1. **Safety & Validation Agent**: Sanitizes descriptions, enforces content moderation, and detects prompt injection attacks.
2. **Planner Agent**: Acts as the coordinator, decomposing the maintenance objective into 4 phased, sequenced steps with explicit agent delegation.
3. **Gym Domain Analysis Agent**: Analyzes the equipment failure mode, identifies parts, and calculates labor/part repair costs using tools.
4. **Action & Execution Agent**: Evaluates the human approval status; if approved, executes repair order creation, dispatches vendor RFQs, and updates ticket status.

### Q6.2: How do you prevent prompt injection? (Golden Case 5)
**Answer**:
The `SafetyValidationAgent` executes a deterministic pattern scanner and input sanitization layer before any prompt is passed to downstream agents. If a prompt contains adversarial instructions (e.g. `IGNORE ALL PREVIOUS INSTRUCTIONS`), the tokens are stripped, logged as an attack attempt, and the issue description is sanitized. Downstream agents only receive the sanitized text.

### Q6.3: How is the Human-in-the-Loop financial approval gate enforced? Can the AI bypass it?
**Answer**:
In LangGraph, `_route_after_planner` and `_approval_gate_node` check the estimated repair cost against `AI_APPROVAL_COST_THRESHOLD` (LKR 10,000 / LKR 25,000). If cost exceeds the threshold:
1. The workflow transitions to status `AwaitingApproval`.
2. Execution in LangGraph is halted (`paused`).
3. An approval record is created in PostgreSQL.
4. Even if the Action Agent is invoked directly, `SendVendorEmailTool` checks `approval_status == 'APPROVED'` and verifies the database. If unapproved, it throws `PermissionError`. The AI physically cannot bypass the database-backed gate.

### Q6.4: What happens if the third-party vendor email service fails? (Golden Case 7)
**Answer**:
`SendVendorEmailTool` wraps the external SMTP/API call in a resilient try/catch block. If a network timeout or 5xx occurs, the repair order in PostgreSQL remains safely committed, a degraded warning is logged in `ai_tool_executions`, and the workflow finishes with a degraded warning. The database transaction is never rolled back.

### Q6.5: How is idempotency enforced on external vendor emails? (Golden Case 8)
**Answer**:
Every action execution tool generates a deterministic idempotency key formatted as `vendor-action-{issue_id}-{action_type}`. Before dispatching an email, it queries `ai_tool_executions` in PostgreSQL for that key. If already present, it immediately returns the cached result without re-sending the email.

---

## 7. Testing, Performance, CI/CD & Deployment

### Q7.1: How many automated tests exist, and what are the pass rates?
**Answer**:
We have **254 automated test assertions** across 4 suites with a **100% pass rate**:
- Backend: 103 xUnit tests
- React: 40 Vitest tests
- Flutter: 33 widget and unit tests
- AI Microservice: 78 pytest tests (covering all 8 Golden Cases)

### Q7.2: What are the measured performance numbers?
**Answer**:
- Database query latency: **0.304 ms mean**, **0.111 ms P50**.
- Concurrent health requests: **100% success rate** across 50 simultaneous requests, 2.3ms mean latency.
- Concurrent AI microservice execution: **100% success rate** across 24 workflows under 8 parallel workers, mean duration 8,032 ms.

### Q7.3: Explain the GitHub Actions CI pipeline.
**Answer**:
Defined in `.github/workflows/ci.yml`. On push or pull request to `main`:
1. `backend-ci`: Restores, builds Release, and runs 103 xUnit tests.
2. `ai-service-ci`: Sets up Python 3.12, installs dependencies, runs 78 pytest tests.
3. `frontend-ci`: Sets up Node 20, runs 40 React tests, builds Vite bundle.
4. `flutter-ci`: Sets up Flutter 3.24, runs 33 Flutter tests.
5. `docker-build-ci`: Builds Docker images for API, Web, and AI.
Merges are blocked if any job fails.
