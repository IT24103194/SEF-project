# ADR-005: Deployment Platform & Containerization Architecture

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: DevOps & Infrastructure Team  
**Consulted**: Systems Architect  

---

## 1. Context & Problem Statement
SmartGym is a polyglot system composed of:
- ASP.NET Core 8 Web API (C#)
- PostgreSQL 16 Database
- React 18 Web Admin (Vite + Nginx)
- FastAPI / LangGraph Microservice (Python 3.13)
- Flutter 3.24 Mobile Client (Dart)

The deployment strategy must provide:
- Deterministic, one-command local reproduction for development and academic viva grading.
- Clean container isolation with zero port collisions or environmental drift.
- A seamless path to production cloud hosting.

---

## 2. Options Considered
1. **Docker Compose Multi-Container Architecture (with Hybrid Cloud Deployment Path)**:
   - Local: Docker Compose orchestrating `smartgym-api`, `smartgym-web`, `smartgym-ai`, and `smartgym-db`.
   - Production: Managed PostgreSQL + Linux Container Apps (Azure App Service / AWS ECS) + Static SPA hosting (Nginx / Vercel) + Standalone Android APK.
2. **Kubernetes (k8s / k3s)**: Full container orchestration with Helm charts and ingress controllers.
3. **Manual Host Installation**: Installing .NET SDK, PostgreSQL, Python, and Node on host machines.
4. **Serverless (AWS Lambda / Azure Functions)**: Decomposing all backend and AI operations into serverless function triggers.

---

## 3. Decision
We chose **Option 1: Docker Compose Multi-Container Architecture**.
- Each service has an optimized multi-stage `Dockerfile`:
  - `backend/SmartGym.Api/Dockerfile`: Multi-stage build compiling .NET 8 DLL and running on `mcr.microsoft.com/dotnet/aspnet:8.0`.
  - `frontend-web/smartgym-web/Dockerfile`: Multi-stage build producing static Vite assets served via `nginx:alpine`.
  - `ai-service/Dockerfile`: Python 3.12/3.13 slim container running Uvicorn.
- `docker-compose.yml` ties all 4 tiers together with health checks, environment injection, and internal networking.
- Mobile client builds standalone release APK via `flutter build apk --release`.

---

## 4. Consequences
### Positive Consequences:
- **Zero Environmental Drift**: Guarantees identical dependencies across Windows, macOS, and Linux.
- **Single-Command Startup**: `docker compose up --build` spins up the entire backend, AI engine, database, and web console.
- **Academic Grading Convenience**: Evaluators can run the entire system locally without configuring external cloud subscriptions.
- **Production Portability**: Container images deploy directly to Azure Container Apps, AWS ECS, or Render.

### Negative Consequences:
- Requires Docker Desktop or Docker Engine installed on the host.

---

## 5. Rejected Alternatives
- **Kubernetes**: Rejected due to disproportionate operational complexity for a university assignment project.
- **Manual Host Installation**: Rejected because manual version discrepancies (.NET versions, Python virtualenvs, Node versions) cause brittle deployments.
- **Serverless**: Rejected because LangGraph state machines and EF Core relational pools perform better with warm persistent container runtimes than cold-start lambda environments.
