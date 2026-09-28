# SmartGym Deployment Architecture & Operations Guide

## 1. Local Multi-Container Deployment (Docker Compose)
The entire 4-tier system can be run locally using Docker Compose:
```bash
# 1. Ensure environment variables are configured
cp .env.example .env

# 2. Build and launch all 4 services
docker compose up --build
```

### Container Endpoints:
- **Web Admin Console**: `http://localhost:3000`
- **ASP.NET Core Web API**: `http://localhost:5000` (Swagger: `/swagger`)
- **FastAPI AI Service**: `http://localhost:8000` (Docs: `/docs`)
- **PostgreSQL Database**: `localhost:5432`

## 2. Cloud Production Deployment Architecture
- **Backend API**: Azure App Service / Render / AWS ECS (Linux container running .NET 8)
- **Database**: Managed PostgreSQL (Azure Database for PostgreSQL / Supabase / AWS RDS)
- **AI Microservice**: Cloud Run / Render / AWS Lambda / ECS
- **Web Admin**: Vercel / Netlify / Cloudflare Pages (Static SPA built from React Vite)
- **Mobile Client**: Standalone Android APK built via Flutter SDK
