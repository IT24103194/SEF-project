# SMARTGYM: Integrated Gym, Supplement, Membership & Facility Management System with Agentic AI

An integrated, enterprise-grade, polyglot software engineering frameworks project designed and built according to the **SmartGym Master Specification** and the **SE3090 Assignment 1** academic baseline.

---

## 🏛️ System Overview & Architecture

SmartGym is a unified gym-management ecosystem that combines membership management, class scheduling, supplement inventory, member feedback, facility maintenance, and a multi-agent Agentic AI workflow for resolving high-impact facility problems.

### Core Architectural Rule
```
[React 18 Web Admin] ───┐
                        ├───► [ASP.NET Core 8 Web API] ───► [PostgreSQL 16 Relational DB]
[Flutter 3 Mobile App] ─┘                │
                                         ▼
                            [Internal Agentic AI Service]
                                   (FastAPI + LangGraph)
                                         │
                                         ▼
                                   [LLM Provider]
                             (Configurable: OpenAI / Anthropic / Gemini / Mock)
```

- **ASP.NET Core Web API** is the sole authoritative public application backend.
- **React and Flutter** NEVER connect directly to PostgreSQL, the Python AI microservice, or external third-party transactional email APIs.
- All business logic, authorization policies, and database constraints are enforced server-side.

---

## 👥 User Roles & Permissions

| Role | Responsibility | Access Channels |
| --- | --- | --- |
| **MEMBER** | Enrolls in memberships, tracks fitness goals & progress, browses and books scheduled classes, submits facility issues with photos, and views repair progress in real time. | Mobile App (Flutter), Web Member Portal |
| **TRAINER** | Manages trainer profile & availability, views assigned classes and schedules, takes attendance for class bookings. | Mobile App, Web Portal |
| **ADMIN / FACILITY MANAGER** | Oversees supplement inventory, CSV import/export, supplier orders, moderates user feedback, reviews AI-generated facility resolution workflows, and authorizes/rejects high-impact repair orders exceeding the financial approval threshold (Rs. 25,000). | Web Admin Console (React) |

---

## 🧩 Four Primary Functional Components

1. **Supplier & Supplement Inventory Management**
   - Full CRUD on suppliers, product categories, and supplement inventory items.
   - Low-stock threshold detection and automated restocking triggers.
   - CSV bulk import and export of inventory records.
   - Auditable inventory adjustment history.

2. **Feedback & Facility Resolution (Agentic AI Core)**
   - Member equipment problem reporting with photos and notes.
   - Equipment catalog with preventive maintenance logs.
   - Multi-agent AI triage, safety validation, spare parts estimation, and technician scheduling.
   - Human-in-the-Loop (HITL) approval gate for repair orders exceeding Rs. 25,000.
   - Real-time resolution timeline updates broadcast to mobile users.

3. **Class Scheduling & Booking**
   - Fitness class definitions, category tags, and duration specifications.
   - Weekly and daily class schedule slots with assigned trainers and rooms.
   - Member booking with real-time capacity enforcement and cancellation policies.
   - Attendance tracking by trainers.

4. **Membership & Goal Tracking**
   - Tiered membership plans (e.g., Bronze, Silver, Gold, Platinum).
   - Subscription lifecycle management (activation, renewal, expiry, cancellation).
   - Member personal fitness goals (target weight, body fat %, muscle mass, milestones).
   - Progress logging with historical metrics tracking.

---

## 🤖 Agentic AI Microservice (Python + FastAPI + LangGraph)

The AI subsystem consists of 4 distinct specialized agents orchestrated with LangGraph:
1. **Planner Agent (Coordinator)**: Analyzes the facility issue, breaks down the remediation plan into structured phases, and coordinates sub-agents.
2. **Safety & Business Validation Agent**: Performs deterministic safety checks, verifies gym operational protocols, and enforces gym warranty and supplier compliance.
3. **Gym Domain Analysis Agent**: Diagnoses mechanical/electronic equipment failure modes, identifies required spare parts, and calculates labor and parts costs.
4. **Action / Tool Agent**: Interacts with external supplier catalogs, drafts repair orders, and dispatches transactional emails upon approval.
- **Human-in-the-Loop (HITL) Financial Gate**: If estimated repair cost exceeds **Rs. 25,000**, execution safely pauses with state persisted in PostgreSQL until an Admin approves or rejects the action via the React Admin console.

---

## 📁 Repository Structure

```
SmartGym/
├── backend/
│   └── SmartGym.Api/             # ASP.NET Core 8 Web API
│       ├── Controllers/          # REST API Controllers (Clean RFC 7807)
│       ├── Data/                 # EF Core DbContext & Configurations
│       ├── DTOs/                 # Request & Response Contracts
│       ├── Entities/             # Relational Domain Models (34 Entities)
│       ├── Middleware/           # Global Exception & Logging Middleware
│       └── Services/             # Domain Business Logic & AI Client
├── ai-service/
│   └── smartgym_ai/              # FastAPI + LangGraph Multi-Agent Service
│       ├── agents/               # 4 Specialized LangGraph Agents
│       ├── api/                  # FastAPI Endpoints & Health Checks
│       ├── orchestration/        # Workflow Graph & State Definitions
│       └── tools/                # Allow-listed Action Tools
├── frontend-web/
│   └── smartgym-web/             # React 18 Web Admin Console (Vite)
├── mobile/
│   └── smartgym_mobile/          # Flutter 3 Cross-Platform Mobile Client
├── tests/
│   ├── backend/                  # xUnit Unit & Integration Tests
│   ├── frontend/                 # React Vitest & Component Tests
│   ├── mobile/                   # Flutter Widget & Unit Tests
│   ├── ai/                       # pytest for AI Agents & Guardrails
│   └── performance/              # k6 / Load Testing Scripts
├── database/
│   ├── scripts/                  # SQL Migrations & Schema Scripts
│   ├── seed/                     # Development Seed Data
│   └── diagrams/                 # Database ERDs
├── docs/
│   ├── adr/                      # Architecture Decision Records
│   ├── architecture/             # System Architecture Specifications
│   ├── specifications/           # Authoritative Master Spec & Assignment Brief
│   └── report/                   # Academic Viva & Assessment Reports
├── .github/workflows/ci.yml      # Automated GitHub Actions CI Pipeline
├── docker-compose.yml            # Multi-Container Local Orchestration
├── .env.example                  # Environment Configuration Template
└── README.md                     # Monorepo Master Documentation
```

---

## 🛠️ Technology Stack Justification

| Layer | Technology | Justification |
| --- | --- | --- |
| **Backend** | ASP.NET Core 8 (C#) | High performance, strict type safety, built-in dependency injection, enterprise-grade security, EF Core integration. |
| **Database** | PostgreSQL 16 + EF Core | Open-source relational standard, strong ACID compliance, robust indexing, seamless migration tooling with Npgsql. |
| **Web Admin** | React 18 + Vite | Fast developer experience, declarative component lifecycle, broad ecosystem, responsive CSS design. |
| **Mobile App** | Flutter 3 + Dart | Single codebase compiling natively to Android and iOS, high frame-rate custom rendering, secure storage. |
| **Agentic AI** | Python 3.12 + FastAPI + LangGraph | Native graph-based stateful agent orchestration, conditional routing, human-in-the-loop interruption, wide LLM compatibility. |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+ & npm](https://nodejs.org/)
- [Python 3.11+](https://www.python.org/)
- [PostgreSQL 16+](https://www.postgresql.org/) or [Docker Desktop](https://www.docker.com/)

### 1. Environment Configuration
Copy the template environment file:
```bash
cp .env.example .env
```

### 2. Running via Docker Compose
To run the entire multi-container stack:
```bash
docker compose up --build
```

### 3. Local Development (Service by Service)
#### Backend Web API:
```bash
cd backend/SmartGym.Api
dotnet restore
dotnet run
# API Swagger UI available at: http://localhost:5000/swagger
```

#### Running Tests:
```bash
dotnet test SmartGym.sln
```

---

## 📜 Academic Integrity & Viva Defense
This project adheres strictly to university guidelines for SE3090:
- All core business rules and agent logic are written directly in code.
- No fabricated test results, git history, or evaluation logs.
- Full architectural transparency with Architectural Decision Records (ADRs) in `docs/adr/`.
