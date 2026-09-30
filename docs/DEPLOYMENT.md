# SmartGym Production Deployment & Operations Guide

## 1. System Overview
SmartGym is an enterprise full-stack gym management platform integrated with an agentic multi-agent maintenance AI pipeline.

### Verified Deployment Endpoints
| Component | Runtime / Technology | Real Endpoint URL | Description |
| :--- | :--- | :--- | :--- |
| **Backend Health** | ASP.NET Core 8 Web API | `http://localhost:5000/health` | System health check (Returns `200 OK - Healthy`) |
| **API Documentation** | Swagger / OpenAPI 3.0 | `http://localhost:5000/swagger` | Interactive Swagger UI for all REST controllers |
| **Web Admin Console** | React 18 / Vite / Nginx | `http://localhost:3000` | Administrative dashboard & Human-in-the-Loop approvals |
| **AI Microservice Health**| FastAPI / Python 3.13 | `http://localhost:8000/health` | Multi-agent LangGraph maintenance service health check |
| **AI Interactive Docs** | FastAPI Swagger | `http://localhost:8000/docs` | OpenAPI documentation for AI workflow endpoints |
| **PostgreSQL Database** | PostgreSQL 16 Relational Engine | `localhost:5432` | Primary transactional and audit datastore |
| **Mobile Client** | Flutter 3.24 (Android) | Native APK | `build/app/outputs/flutter-apk/app-release.apk` |

---

## 2. Environment Variables Reference

### Backend API (.NET 8)
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string (e.g. `Host=localhost;Port=5432;Database=smartgym;Username=postgres;Password=1234;Include Error Detail=true`)
- `JwtSettings__Secret`: 256-bit cryptographically secure secret string for HMAC-SHA256 signature verification.
- `JwtSettings__Issuer`: `SmartGymApi`
- `JwtSettings__Audience`: `SmartGymClients`
- `JwtSettings__AccessTokenExpirationMinutes`: `15`
- `JwtSettings__RefreshTokenExpirationDays`: `7`
- `AiService__BaseUrl`: URL of the Python AI microservice (`http://localhost:8000` or `http://smartgym-ai:8000`)
- `Cors__AllowedOrigins`: Comma-separated allowed frontend origins (`http://localhost:3000,http://localhost:5173`)

### AI Microservice (FastAPI / LangGraph)
- `DATABASE_URL`: PostgreSQL connection string (`postgresql://postgres:1234@localhost:5432/smartgym`)
- `AI_SERVICE_PORT`: Port to listen on (default: `8000`)
- `AI_APPROVAL_COST_THRESHOLD`: Cost in LKR triggering human approval gate (default: `10000.0`)
- `OPENAI_API_KEY`: API key for OpenAI LLM provider (optional in mock mode)
- `ANTHROPIC_API_KEY`: API key for Anthropic LLM provider (optional in mock mode)
- `LLM_PROVIDER`: Selected LLM provider (`mock`, `openai`, `anthropic`, `gemini`)

### Frontend Web (React + Vite)
- `VITE_API_URL`: Base URL of ASP.NET Core API (`http://localhost:5000/api`)

### Mobile App (Flutter)
- `API_BASE_URL`: Base URL of ASP.NET Core API (`http://10.0.2.2:5000/api` for Android emulator, or `http://localhost:5000/api` for desktop/web)

---

## 3. Database Deployment Evidence
PostgreSQL schema is managed through Entity Framework Core Code-First migrations with full relational integrity, foreign key constraints, and indexing.

### Active Relational Schema (36 Tables)
1. `__EFMigrationsHistory`
2. `users`
3. `roles`
4. `user_roles`
5. `refresh_tokens`
6. `members`
7. `memberships`
8. `membership_plans`
9. `locations`
10. `equipment`
11. `class_categories`
12. `fitness_classes`
13. `class_schedules`
14. `bookings`
15. `attendances`
16. `facility_issues`
17. `issue_images`
18. `repair_orders`
19. `repair_order_items`
20. `product_categories`
21. `products`
22. `inventory_items`
23. `stock_movements`
24. `suppliers`
25. `purchase_orders`
26. `purchase_order_items`
27. `goals`
28. `progress_records`
29. `feedbacks`
30. `notifications`
31. `audit_logs`
32. `ai_workflows`
33. `ai_workflow_steps`
34. `ai_validation_results`
35. `ai_tool_executions`
36. `approvals`

### Seeded Credentials
- **Admin**: `admin@smartgym.com` / `Admin123!`
- **Manager**: `manager@smartgym.com` / `Manager123!`
- **Trainer**: `trainer@smartgym.com` / `Trainer123!`
- **Member**: `member@smartgym.com` / `Member123!`

---

## 4. Startup & Execution Instructions

### Option A: Local Multi-Container Deployment (Docker Compose)
Launch the entire system stack in isolated containers with automatic health-checking:
```bash
# 1. Start all containers in detached mode
docker compose up -d --build

# 2. View container health and logs
docker compose ps
docker compose logs -f

# 3. Stop containers
docker compose down
```

### Option B: Local Host Process Execution

#### 1. PostgreSQL Database
Ensure PostgreSQL is running locally on port 5432:
```bash
# PostgreSQL connection: postgresql://postgres:1234@localhost:5432/smartgym
```

#### 2. ASP.NET Core 8 Web API
```bash
cd backend/SmartGym.Api
dotnet restore
dotnet run --configuration Release
# API listens on http://localhost:5000
```

#### 3. Python AI Microservice
```bash
cd ai-service
pip install -r requirements.txt
uvicorn smartgym_ai.main:app --host 0.0.0.0 --port 8000 --reload
# Microservice listens on http://localhost:8000
```

#### 4. React 18 Web Admin
```bash
cd frontend-web/smartgym-web
npm install
npm run dev
# Web application available at http://localhost:5173 (dev) or http://localhost:3000 (docker/preview)
```

#### 5. Flutter Mobile Application (APK Build)
```bash
cd mobile/smartgym_mobile
flutter pub get
flutter test
flutter build apk --release
# Generated APK: mobile/smartgym_mobile/build/app/outputs/flutter-apk/app-release.apk
```

---

## 5. End-to-End Verification Pipeline
The system completes the full cross-platform workflow:
1. **Flutter Mobile**: Member captures broken treadmill photo and submits facility issue.
2. **ASP.NET Core API**: Authenticates JWT, enforces RBAC, validates request, and persists record in PostgreSQL `facility_issues`.
3. **AI Microservice**: Safety Agent validates and sanitizes input; Planner Agent creates 4-step execution plan; Domain Analysis Agent executes gym tool queries; Action Agent formulates repair order proposal and flags workflow as `AwaitingApproval`.
4. **React Web Admin**: Approval dashboard detects pending repair order; Admin reviews diagnostic summary and cost estimate (LKR 15,000) and clicks **Approve**.
5. **ASP.NET Core API**: Authorizes approval, persists audit log, and resumes workflow execution.
6. **Action Agent Execution**: Automatically dispatches vendor RFQ email and updates facility issue status to `VENDOR_CONTACTED`.
7. **Flutter Mobile**: Member views updated status in real time.
