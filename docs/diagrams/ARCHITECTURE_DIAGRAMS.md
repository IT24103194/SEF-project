# SmartGym Architecture Diagrams Specification

This document presents the complete architectural diagram suite for SmartGym, modeled using GitHub-compatible Mermaid syntax.

---

## 1. Overall System Architecture
Shows the physical and logical boundaries of the 4-tier ecosystem.

```mermaid
graph TD
    subgraph Client_Layer ["Client Tier"]
        FlutterApp["Flutter 3.24 Mobile Client<br/>(Members & Trainers)"]
        ReactAdmin["React 18 Web Admin Console<br/>(Managers & Admins)"]
    end

    subgraph Gateway_Layer ["Authoritative Application Gateway"]
        AspNetCoreAPI["ASP.NET Core 8 Web API<br/>(Controllers, Middleware, Business Services)"]
    end

    subgraph Data_Layer ["Persistence Tier"]
        PostgresDB[("PostgreSQL 16 Database<br/>(36 Relational Tables & Migrations)")]
    end

    subgraph AI_Layer ["Autonomous Multi-Agent AI Tier"]
        FastApiAI["FastAPI AI Microservice<br/>(Python 3.13 + LangGraph)"]
        subgraph Agents ["LangGraph State Machine"]
            SafetyAgent["Safety & Content Agent"]
            PlannerAgent["Planner Coordinator Agent"]
            DomainAgent["Gym Domain Analysis Agent"]
            ActionAgent["Action & Tool Execution Agent"]
        end
    end

    subgraph External_Layer ["External Integration"]
        EmailService["Transactional Supplier Email Service<br/>(Vendor RFQ Dispatch)"]
        LLMProvider["Configurable LLM Provider<br/>(Mock / OpenAI / Anthropic / Gemini)"]
    end

    %% Client communication
    FlutterApp -->|"HTTPS / REST (JWT)"| AspNetCoreAPI
    ReactAdmin -->|"HTTPS / REST (JWT)"| AspNetCoreAPI

    %% Backend persistence & orchestration
    AspNetCoreAPI -->|"Entity Framework Core (Npgsql)"| PostgresDB
    AspNetCoreAPI -->|"Private REST / JSON (X-Correlation-ID)"| FastApiAI

    %% AI internal orchestration
    FastApiAI --> Agents
    SafetyAgent --> PlannerAgent
    PlannerAgent --> DomainAgent
    DomainAgent --> ActionAgent
    Agents -->|"Direct Relational Tools (Read-Only)"| PostgresDB
    Agents -->|"Prompts & Structured Schema"| LLMProvider

    %% Action tool dispatch
    ActionAgent -->|"Dispatches Approved RFQ"| EmailService
```

---

## 2. Database Architecture
Illustrates core data domains and relationships in PostgreSQL 16.

```mermaid
graph LR
    subgraph Identity_Domain ["Identity & Security"]
        Users["users"] --- UserRoles["user_roles"]
        Roles["roles"] --- UserRoles
        Users --- RefreshTokens["refresh_tokens"]
    end

    subgraph Member_Domain ["Members & Subscriptions"]
        Users --- Members["members"]
        Members --- Memberships["memberships"]
        MembershipPlans["membership_plans"] --- Memberships
        Members --- Goals["goals"]
        Members --- ProgressRecords["progress_records"]
    end

    subgraph Operations_Domain ["Class Scheduling & Operations"]
        Classes["fitness_classes"] --- Schedules["class_schedules"]
        Locations["locations"] --- Schedules
        Schedules --- Bookings["bookings"]
        Bookings --- Attendances["attendances"]
    end

    subgraph Inventory_Domain ["Supplements & Inventory"]
        Products["products"] --- Inventory["inventory_items"]
        Inventory --- StockMovements["stock_movements"]
        Suppliers["suppliers"] --- PurchaseOrders["purchase_orders"]
        PurchaseOrders --- POItems["purchase_order_items"]
    end

    subgraph Facility_Domain ["Facility & Multi-Agent AI"]
        Equipment["equipment"] --- FacilityIssues["facility_issues"]
        FacilityIssues --- IssueImages["issue_images"]
        FacilityIssues --- AIWorkflows["ai_workflows"]
        AIWorkflows --- Steps["ai_workflow_steps"]
        Steps --- ToolExecutions["ai_tool_executions"]
        AIWorkflows --- Approvals["approvals"]
        FacilityIssues --- RepairOrders["repair_orders"]
    end
```

---

## 3. ASP.NET Core Layered Architecture
Depicts Clean Architecture separation of concerns within `SmartGym.Api`.

```mermaid
graph TD
    subgraph Presentation_Layer ["Presentation Layer"]
        Controllers["Controllers (RFC 7807 ProblemDetails)<br/>Auth, FacilityIssues, Approvals, Inventory, AIWorkflows"]
        Swagger["OpenAPI 3.0 / Swagger UI"]
    end

    subgraph Middleware_Pipeline ["HTTP Pipeline & Middleware"]
        CorsMiddleware["CORS Policy Middleware"]
        AuthMiddleware["JWT Authentication & RBAC Middleware"]
        ExceptionMiddleware["Global Exception & RFC 7807 Middleware"]
    end

    subgraph Application_Layer ["Application & Domain Services"]
        AuthService["AuthService (Token Generation & Password Hashing)"]
        FacilityService["FacilityIssueService (CRUD & Workflow Dispatch)"]
        ApprovalService["ApprovalService (HITL Gate Validation)"]
        InventoryService["InventoryService (Atomic Restock & Auditing)"]
        AiClientService["AiServiceClient (HTTP Resilience & Correlation)"]
    end

    subgraph Data_Access_Layer ["Data Access Layer (Infrastructure)"]
        DbContext["SmartGymDbContext (EF Core 8)"]
        NpgsqlProvider["Npgsql PostgreSQL Driver"]
    end

    Controllers --> Middleware_Pipeline
    Middleware_Pipeline --> Application_Layer
    Application_Layer --> Data_Access_Layer
```

---

## 4. React Web Admin Architecture
Details component composition, Redux store slices, and protected routing.

```mermaid
graph TD
    subgraph Routing ["React Router v6 Routing"]
        AppRoutes["AppRoutes (Protected Route Guards)"]
        LoginPage["LoginPage (Public)"]
        DashboardLayout["DashboardLayout (Sidebar, TopNav, NotificationBadge)"]
    end

    subgraph Pages ["Admin Views & HITL Interfaces"]
        ApprovalsPage["ApprovalsPage (Human Approval Gate)"]
        FacilityPage["FacilityResolutionPage (Issue Timeline & Filters)"]
        InventoryPage["InventoryPage (Stock Grid & Restock Modal)"]
        ClassSchedulePage["ClassSchedulePage (Timetable Grid)"]
        ReportsPage["ReportsPage (Operational KPI Visualizations)"]
    end

    subgraph Redux_State ["Redux Toolkit Store"]
        AuthSlice["authSlice (Token, UserProfile, Role)"]
        ApprovalSlice["approvalSlice (Pending Approvals, Actions)"]
        FacilitySlice["facilitySlice (Issues, Statuses)"]
        InventorySlice["inventorySlice (Products, Low-Stock Alerts)"]
    end

    subgraph Services ["Axios HTTP Client"]
        ApiInterceptor["Axios Interceptor (Bearer JWT Injection & 401 Catch)"]
    end

    AppRoutes --> LoginPage
    AppRoutes --> DashboardLayout
    DashboardLayout --> Pages
    Pages --> Redux_State
    Redux_State --> Services
```

---

## 5. Flutter Mobile Architecture
Illustrates Riverpod state management, secure storage, and screen navigation.

```mermaid
graph TD
    subgraph UI_Screens ["Flutter Screen Widgets"]
        LoginScreen["LoginScreen (Biometric & Credential Auth)"]
        FacilityReportScreen["FacilityReportScreen (Photo Picker & Notes)"]
        FacilityDetailScreen["FacilityIssueDetailScreen (Timeline Stream)"]
        ClassesScreen["ClassesScreen (Booking Grid & Spots Left)"]
        TrainerDashboardScreen["TrainerDashboardScreen (Attendance Check-In)"]
        MembershipScreen["MembershipScreen (Renewal Bottom Sheet)"]
    end

    subgraph State_Management ["Riverpod Providers"]
        AuthProvider["authNotifierProvider (Session State)"]
        FacilityProvider["facilityIssuesProvider (Issue List & Stream)"]
        BookingProvider["bookingNotifierProvider (Atomic Booking)"]
        AttendanceProvider["attendanceProvider (Trainer Actions)"]
    end

    subgraph Local_Storage ["Security & Device Layer"]
        SecureStorage["SecureStorageService (FlutterSecureStorage / Keystore)"]
        CameraService["ImagePicker (Camera & Gallery Media Access)"]
    end

    subgraph Network_Layer ["HTTP Client"]
        ApiClient["ApiClient (HTTP Headers, Error Mapping)"]
    end

    UI_Screens --> State_Management
    UI_Screens --> CameraService
    State_Management --> SecureStorage
    State_Management --> Network_Layer
```

---

## 6. Agentic AI Multi-Agent Architecture
Demonstrates the 4 distinct specialized agents and their interaction with the LangGraph state machine.

```mermaid
graph TD
    StartNode(["Start"]) --> InitNode["_initialize_node<br/>(Create Initial State & Correlation ID)"]
    InitNode --> PlannerNode["Planner Agent Node<br/>(Decomposes Goals into 4 Phased Steps)"]

    PlannerNode --> SafetyNode["Safety Validation Agent Node<br/>(Prompt Injection Sanitization & Rule Checks)"]

    SafetyNode -->|"Safety Passed"| DomainNode["Gym Domain Analysis Agent Node<br/>(Invokes getEquipmentDetails, checkInventory, getSupplierDetails)"]
    SafetyNode -->|"Safety Failed"| SafeFailureNode["_safe_failure_node<br/>(Escalate to Technician On-Site Review)"]

    DomainNode --> ApprovalGateNode{"Approval Gate<br/>Cost >= LKR 10,000?"}

    ApprovalGateNode -->|"Yes (Cost >= Threshold)"| PauseNode["_approval_gate_node<br/>(Status: AwaitingApproval -> Pause Execution)"]
    ApprovalGateNode -->|"No (Auto-Approve)"| ExecutionNode["Action Execution Agent Node<br/>(createRepairOrder, sendVendorEmail, updateStatus)"]

    PauseNode -.->|"Human Admin Approves"| ResumeNode["_route_after_gate<br/>(Admin Approval Confirmed)"]
    ResumeNode --> ExecutionNode

    ExecutionNode --> CompletedNode(["End (Status: Completed)"])
    SafeFailureNode --> EndFailed(["End (Status: Failed)"])
```

---

## 7. Production & Local Deployment Architecture
Shows Docker Compose containerization and port mapping.

```mermaid
graph TD
    subgraph Host_Environment ["Docker Host / VM"]
        subgraph Frontend_Container ["smartgym-web Container (Port 3000)"]
            Nginx["Nginx Reverse Proxy & Static Host"]
            ReactBundle["React SPA Static Files"]
            Nginx --> ReactBundle
        end

        subgraph Backend_Container ["smartgym-api Container (Port 5000)"]
            Kestrel["Kestrel Web Server (.NET 8)"]
            CoreApiApp["SmartGym.Api.dll"]
            Kestrel --> CoreApiApp
        end

        subgraph AI_Container ["smartgym-ai Container (Port 8000)"]
            Uvicorn["Uvicorn ASGI Server (Python 3.13)"]
            FastApiApp["FastAPI + LangGraph Engine"]
            Uvicorn --> FastApiApp
        end

        subgraph Database_Container ["smartgym-db Container (Port 5432)"]
            PostgreEngine["PostgreSQL 16 Server"]
            VolumeMount[("Persistent Data Volume: pgdata")]
            PostgreEngine --- VolumeMount
        end
    end

    UserWeb(("Web Browser")) -->|"http://localhost:3000"| Nginx
    UserMobile(("Mobile Device")) -->|"http://localhost:5000/api"| Kestrel
    Nginx -->|"Proxies API requests"| Kestrel
    CoreApiApp -->|"Connects to 5432"| PostgreEngine
    CoreApiApp -->|"Private REST HTTP: 8000"| Uvicorn
    FastApiApp -->|"Direct read-only tools: 5432"| PostgreEngine
```

---

## 8. Authentication & Authorization Flow (JWT & RBAC)
Details the dual-token issuance, verification, and role validation.

```mermaid
sequenceDiagram
    autonumber
    actor Client as Flutter / React Client
    participant AuthCtrl as AuthController
    participant AuthService as AuthService
    participant Hasher as PasswordHasher
    participant DB as PostgreSQL
    participant Guard as JwtBearerHandler

    Client->>AuthCtrl: POST /api/auth/login (email, password)
    AuthCtrl->>AuthService: ValidateCredentialsAsync(email, password)
    AuthService->>DB: Query User & Roles by Email
    DB-->>AuthService: User entity + PasswordHash + Roles
    AuthService->>Hasher: VerifyHashedPassword(hash, inputPassword)
    Hasher-->>AuthService: Success (Password matches)
    AuthService->>AuthService: GenerateAccessToken (15m, HMAC-SHA256, Claims)
    AuthService->>AuthService: GenerateRefreshToken (7d, Secure Random String)
    AuthService->>DB: Insert RefreshToken record
    AuthService-->>AuthCtrl: AuthResponse DTO
    AuthCtrl-->>Client: 200 OK (AccessToken, RefreshToken, Roles)

    Note over Client, Guard: Subsequent Authenticated Requests
    Client->>Guard: GET /api/approvals (Authorization: Bearer <AccessToken>)
    Guard->>Guard: Validate Signature, Lifetime, Issuer, Audience
    Guard->>Guard: Evaluate [Authorize(Roles = "Admin,Manager")]
    alt User has required role
        Guard->>AuthCtrl: Proceed to Controller Action
        AuthCtrl-->>Client: 200 OK (Data Payload)
    else User is Member
        Guard-->>Client: 403 Forbidden (Insufficient Privileges)
    else Token Expired
        Guard-->>Client: 401 Unauthorized
    end
```

---

## 9. Main AI Maintenance Workflow
End-to-end trace from issue reporting to automated resolution.

```mermaid
sequenceDiagram
    autonumber
    actor Member as Member (Flutter)
    participant API as ASP.NET Core API
    participant DB as PostgreSQL
    participant AI as LangGraph Engine
    actor Admin as Admin (React)
    participant Vendor as Vendor Email

    Member->>API: POST /api/facility-issues (Broken Treadmill Photo + Description)
    API->>DB: INSERT into facility_issues (Status: Submitted)
    API->>AI: POST /api/ai-workflows/start (Issue Metadata)
    AI->>AI: Safety Validation Agent (Sanitize Text & Validate Severity)
    AI->>AI: Planner Agent (Generate 4-Step Maintenance Plan)
    AI->>DB: Domain Analysis Agent (Query Equipment Details & Parts Inventory)
    AI->>AI: Action Agent (Estimate Cost: LKR 15,000 -> Threshold Exceeded!)
    AI->>DB: INSERT into approvals (Status: Pending) & UPDATE workflow (Status: AwaitingApproval)
    AI-->>API: 200 OK (Workflow Paused at HITL Gate)

    Admin->>API: GET /api/approvals (View Pending Queue)
    API-->>Admin: Approval Details + AI Diagnosis + Cost Estimate
    Admin->>API: POST /api/approvals/{id}/action (Action: Approve)
    API->>DB: UPDATE approvals (Status: Approved)
    API->>AI: POST /api/ai-workflows/{id}/resume (Approved = true)
    AI->>Vendor: SendVendorEmailTool (Dispatches RFQ to Certified Supplier)
    AI->>DB: INSERT into repair_orders & UPDATE facility_issues (Status: VENDOR_CONTACTED)
    AI-->>API: 200 OK (Workflow Completed)
    Member->>API: GET /api/facility-issues/{id}/workflow-status
    API-->>Member: 200 OK (Status: VENDOR_CONTACTED, Technician Dispatched)
```

---

## 10. Human-in-the-Loop Approval Workflow
Detailed state machine of the financial approval gate.

```mermaid
stateDiagram-v2
    [*] --> IssueReported: Member Submits Ticket
    IssueReported --> AIAnalysis: Automated Diagnosis
    AIAnalysis --> CostEvaluation: Domain Analysis Calculates Cost

    state CostEvaluation <<choice>>
    CostEvaluation --> AutoApproved: Estimated Cost < LKR 10,000
    CostEvaluation --> PendingApproval: Estimated Cost >= LKR 10,000

    PendingApproval --> HumanReview: Visible in React Approvals Dashboard
    HumanReview --> DecisionMade: Admin / Manager Reviews Proposal

    state DecisionMade <<choice>>
    DecisionMade --> Approved: Manager Clicks "Approve"
    DecisionMade --> Rejected: Manager Clicks "Reject"

    Approved --> ActionExecution: Resume AI Workflow -> Dispatch RFQ
    AutoApproved --> ActionExecution: Immediate Execution
    Rejected --> WorkflowTerminated: Log Decision & Notify Reporter

    ActionExecution --> WorkCompleted: Issue Status: VENDOR_CONTACTED
    WorkCompleted --> [*]
    WorkflowTerminated --> [*]
```

---

## 11. Third-Party Integration Workflow
Transactional email adapter execution with degraded mode resilience.

```mermaid
sequenceDiagram
    autonumber
    participant Agent as ActionExecutionAgent
    participant Tool as SendVendorEmailTool
    participant DB as PostgreSQL
    participant EmailService as External Vendor SMTP/API

    Agent->>Tool: Execute(recipient, subject, approval_status, idempotency_key)
    Tool->>DB: Check if idempotency_key already processed in ai_tool_executions
    alt Duplicate Request (Replay Attack / Double Click)
        Tool-->>Agent: Return Cached Execution Result (Idempotency Guard)
    else First Time Request
        alt approval_status == "APPROVED"
            Tool->>EmailService: Dispatch RFQ Email (Recipient, Part, Model)
            alt Email Service Available (200 OK)
                EmailService-->>Tool: MessageID: MSG-92482
                Tool->>DB: INSERT into ai_tool_executions (Status: Success, Duration: 35ms)
                Tool-->>Agent: ToolResult (Success = true)
            else Network Timeout / 5xx Outage
                EmailService--xTool: Connection Timed Out / 503 Service Unavailable
                Tool->>DB: INSERT into audit_logs (Action: EMAIL_DEGRADED_FALLBACK)
                Tool->>DB: INSERT into ai_tool_executions (Status: DegradedSuccess)
                Tool-->>Agent: ToolResult (Success = true, Warning: "Email queued in spooler")
            end
        else approval_status != "APPROVED"
            Tool-->>Agent: Raise PermissionError ("Approval bypass attempt rejected")
        end
    end
```

---

## 12. CI/CD GitHub Actions Pipeline
Visualizes the automated continuous integration workflow executed on push and pull requests.

```mermaid
graph TD
    Trigger(["Git Push / PR to main"]) --> ParallelTests

    subgraph ParallelTests ["GitHub Actions Runner Matrix (ubuntu-latest)"]
        subgraph Backend_Job ["backend-ci"]
            DotnetRestore["dotnet restore"] --> DotnetBuild["dotnet build --configuration Release"]
            DotnetBuild --> DotnetTest["dotnet test (103 xUnit Tests)"]
        end

        subgraph AI_Job ["ai-service-ci"]
            PySetup["Setup Python 3.12"] --> PyInstall["pip install -r requirements.txt"]
            PyInstall --> PyTest["pytest tests/ai (78 Tests & 8 Golden Cases)"]
        end

        subgraph Frontend_Job ["frontend-ci"]
            NodeSetup["Setup Node 20"] --> NpmInstall["npm ci"]
            NpmInstall --> NpmTest["npm test (40 Vitest Component Tests)"]
            NpmTest --> NpmBuild["npm run build (Vite Production Assets)"]
        end

        subgraph Flutter_Job ["flutter-ci"]
            FlutterSetup["Setup Flutter 3.24"] --> FlutterPub["flutter pub get"]
            FlutterPub --> FlutterTest["flutter test (33 Widget & Unit Tests)"]
        end

        subgraph Docker_Job ["docker-build-ci"]
            Buildx["Docker Buildx Setup"] --> BuildApi["Build smartgym-api Image"]
            Buildx --> BuildWeb["Build smartgym-web Image"]
            Buildx --> BuildAI["Build smartgym-ai Image"]
        end
    end

    DotnetTest --> SuccessCheck{"All 5 Jobs Pass?"}
    PyTest --> SuccessCheck
    NpmBuild --> SuccessCheck
    FlutterTest --> SuccessCheck
    BuildAI --> SuccessCheck

    SuccessCheck -->|"Yes (254 Tests Passed)"| DeployReady(["Build Green: Ready for Merge / Release"])
    SuccessCheck -->|"No (Any Failure)"| PipelineFailed(["Build Red: PR Blocked"])
```
