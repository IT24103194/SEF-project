# SmartGym REST API Catalog & Endpoint Overview

Adhering to **Section 28 (API Catalog Summary)** of the SmartGym Master Specification:

## 1. Authentication Endpoints
- `POST /api/auth/register` — Register a new member
- `POST /api/auth/login` — Authenticate and receive JWT + Refresh Token
- `POST /api/auth/refresh` — Refresh expired JWT using valid refresh token
- `POST /api/auth/logout` — Invalidate user session
- `GET /api/auth/me` — Retrieve active authenticated user profile

## 2. Component 1: Supplier & Supplement Inventory Management
- `GET /api/suppliers` — List suppliers with pagination/filtering
- `POST /api/suppliers` — Create a new supplier
- `GET /api/products` — List supplement products
- `POST /api/products` — Create a product
- `GET /api/inventory` — View inventory levels & stock alarms
- `POST /api/inventory/adjust` — Adjust stock quantity (business operation)
- `POST /api/inventory/import-csv` — Bulk import products from CSV
- `GET /api/inventory/export-csv` — Export stock report to CSV

## 3. Component 2: Feedback & Facility Resolution (AI Core)
- `GET /api/equipment` — List gym machines and preventive maintenance logs
- `POST /api/facility-issues` — Member reports equipment problem with location/photo
- `GET /api/facility-issues/{id}` — Track repair workflow status in real time
- `POST /api/ai-workflows/{id}/trigger` — Initiate LangGraph 4-agent triage
- `POST /api/ai-workflows/{id}/approve` — Admin approves high-cost repair order (> Rs. 25,000)
- `POST /api/ai-workflows/{id}/reject` — Admin rejects repair proposal

## 4. Component 3: Class Scheduling & Booking
- `GET /api/classes` — View class catalog
- `GET /api/classes/schedules` — View weekly timetable slots
- `POST /api/bookings` — Reserve class slot with capacity limit check
- `DELETE /api/bookings/{id}` — Cancel class reservation
- `POST /api/attendance` — Trainer marks member attendance

## 5. Component 4: Membership & Goal Tracking
- `GET /api/membership-plans` — View tier pricing (Bronze, Silver, Gold, Platinum)
- `POST /api/memberships` — Subscribe to a plan
- `GET /api/goals` — List personal fitness goals
- `POST /api/goals` — Create fitness target
- `POST /api/goals/{id}/progress` — Log progress metric

## 6. System & Diagnostics
- `GET /health` — ASP.NET Core health check
- `GET /api/system/info` — API metadata and component registry
