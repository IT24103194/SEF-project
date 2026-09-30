# SmartGym Comprehensive REST API Specification

**API Version**: 1.0.0  
**Base URL**: `http://localhost:5000/api` (Swagger UI: `http://localhost:5000/swagger`, Health Check: `http://localhost:5000/health`)  
**Specification Standard**: RFC 7807 (Problem Details for HTTP APIs)  
**Authentication**: Bearer JWT (`Authorization: Bearer <token>`)

---

## 1. Authentication & Identity (`/api/auth`)

### 1.1 Register New Account
- **Method & Route**: `POST /api/auth/register`
- **Authentication**: None (Public)
- **Role Required**: None
- **Request Body**:
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "password": "Password123!",
  "phoneNumber": "+94771234567",
  "role": "Member"
}
```
- **Response**: `201 Created`
```json
{
  "userId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "roles": ["Member"],
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "expiresAt": "2026-10-01T01:00:00Z"
}
```
- **Errors**: `400 Bad Request` (Email already exists / validation failure).

### 1.2 User Login
- **Method & Route**: `POST /api/auth/login`
- **Authentication**: None (Public)
- **Role Required**: None
- **Request Body**:
```json
{
  "email": "admin@smartgym.com",
  "password": "Admin123!"
}
```
- **Response**: `200 OK` (Returns `accessToken`, `refreshToken`, user metadata, and roles).
- **Errors**: `401 Unauthorized` (Invalid email or password).

### 1.3 Refresh Access Token
- **Method & Route**: `POST /api/auth/refresh-token`
- **Authentication**: None
- **Role Required**: None
- **Request Body**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
  "refreshToken": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```
- **Response**: `200 OK` (New `accessToken` and rotated `refreshToken`).
- **Errors**: `401 Unauthorized` (Invalid or revoked refresh token).

### 1.4 Revoke Refresh Token
- **Method & Route**: `POST /api/auth/revoke-token`
- **Authentication**: Bearer JWT
- **Role Required**: Any authenticated user
- **Request Body**: `{"refreshToken": "7c9e6679-7425-40de-944b-e07fc1f90ae7"}`
- **Response**: `204 No Content`

### 1.5 Get Current User Profile
- **Method & Route**: `GET /api/auth/me`
- **Authentication**: Bearer JWT
- **Role Required**: Any authenticated user
- **Response**: `200 OK` (Returns current user's profile, roles, and timestamps).

---

## 2. Facility Issues & AI Workflow Tracking (`/api/facility-issues`)

### 2.1 Submit New Facility Issue (Photo & Diagnostics)
- **Method & Route**: `POST /api/facility-issues`
- **Authentication**: Bearer JWT
- **Role Required**: `Member`, `Trainer`, `Manager`, `Admin`
- **Request Body**:
```json
{
  "title": "Treadmill T12 makes loud grinding noise",
  "description": "The deck rattles and belt slips when running above 8 km/h.",
  "equipmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "locationId": "f1e2d3c4-b5a6-7890-abcd-ef0987654321",
  "severity": 3,
  "imageUrls": ["https://storage.smartgym.com/issues/t12-deck-crack.jpg"]
}
```
- **Response**: `201 Created`
```json
{
  "id": "e43b1772-5bb9-4cfc-84cb-3bf996a6dfa2",
  "title": "Treadmill T12 makes loud grinding noise",
  "status": "Submitted",
  "severity": 3,
  "equipmentName": "Commercial Treadmill T12",
  "reportedAt": "2026-10-01T00:30:00Z",
  "moderationStatus": "Approved"
}
```
- **Errors**: `400 Bad Request` (Invalid equipment or missing required fields).

### 2.2 List Facility Issues (Paginated & Filterable)
- **Method & Route**: `GET /api/facility-issues?pageNumber=1&pageSize=10&status=Submitted&severity=3`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Response**: `200 OK` (Paginated list with total count and records).

### 2.3 Get Facility Issue by ID
- **Method & Route**: `GET /api/facility-issues/{id}`
- **Authentication**: Bearer JWT
- **Role Required**: Authenticated reporter or `Manager`/`Admin`
- **Response**: `200 OK` (Detailed issue data, images, repair orders, and resolution notes).

### 2.4 Get Live AI Workflow Resolution Status
- **Method & Route**: `GET /api/facility-issues/{id}/workflow-status`
- **Authentication**: Bearer JWT
- **Role Required**: Any authenticated user
- **Response**: `200 OK`
```json
{
  "issueId": "e43b1772-5bb9-4cfc-84cb-3bf996a6dfa2",
  "workflowId": "9b9db3bf-6dcd-4a3f-9566-a6681b84097f",
  "currentStatus": "AwaitingApproval",
  "currentAgent": "ActionExecutionAgent",
  "estimatedCost": 15000.0,
  "requiresApproval": true,
  "stepsCompleted": [
    {"name": "Safety Validation", "agent": "SafetyValidationAgent", "status": "Passed"},
    {"name": "Planner Decomposition", "agent": "PlannerAgent", "status": "Completed"},
    {"name": "Gym Domain Analysis", "agent": "DomainAnalysisAgent", "status": "Completed"}
  ]
}
```

---

## 3. Human-in-the-Loop Approvals (`/api/approvals`)

### 3.1 List Pending Approvals
- **Method & Route**: `GET /api/approvals?status=Pending&pageNumber=1&pageSize=10`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Response**: `200 OK`
```json
[
  {
    "id": "7820be44-8cb3-4876-92fc-ea70d740eb76",
    "workflowId": "9b9db3bf-6dcd-4a3f-9566-a6681b84097f",
    "facilityIssueId": "e43b1772-5bb9-4cfc-84cb-3bf996a6dfa2",
    "actionType": "RepairOrderApproval",
    "estimatedCost": 15000.0,
    "status": "Pending",
    "diagnosisSummary": "Drive belt wear or pulley bearing friction",
    "recommendedSupplier": "LifeFitness Certified Logistics",
    "createdAt": "2026-10-01T00:31:00Z"
  }
]
```

### 3.2 Execute Approval Decision (Approve or Reject)
- **Method & Route**: `POST /api/approvals/{id}/action`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Request Body**:
```json
{
  "action": "Approve",
  "notes": "Approved for vendor dispatch by Operations Manager"
}
```
- **Response**: `200 OK`
```json
{
  "approvalId": "7820be44-8cb3-4876-92fc-ea70d740eb76",
  "status": "Approved",
  "reviewedBy": "admin@smartgym.com",
  "reviewedAt": "2026-10-01T00:32:00Z",
  "workflowResumed": true
}
```
- **Errors**: `403 Forbidden` (Member attempting approval), `400 Bad Request` (Already decided).

---

## 4. Multi-Agent AI Workflow Orchestration (`/api/ai-workflows`)

### 4.1 Trigger New Maintenance AI Workflow
- **Method & Route**: `POST /api/ai-workflows/start`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin` (or system internal)
- **Request Body**:
```json
{
  "issueId": "e43b1772-5bb9-4cfc-84cb-3bf996a6dfa2",
  "issueTitle": "Treadmill T12 belt slip",
  "equipmentName": "Commercial Treadmill T12",
  "description": "Drive belt slips above 8 km/h.",
  "workflowType": "FacilityMaintenance",
  "contextData": {"severity": 3, "userRole": "Member"}
}
```
- **Response**: `200 OK` (Returns initial `WorkflowState`, status, and plan).

### 4.2 Resume Paused Workflow Post-Approval
- **Method & Route**: `POST /api/ai-workflows/{id}/resume`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Request Body**:
```json
{
  "approved": true,
  "approverUserId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "approvalNotes": "Proceed with purchase order"
}
```
- **Response**: `200 OK` (Workflow transitions to `Executing` → `Completed`).

### 4.3 Get Workflow Execution Steps & Audit Logs
- **Method & Route**: `GET /api/ai-workflows/{id}/steps`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Response**: `200 OK` (Array of executed agent steps, statuses, and duration in ms).

---

## 5. Supplement & Inventory Management (`/api/inventory`)

### 5.1 List Inventory Items
- **Method & Route**: `GET /api/inventory?pageNumber=1&pageSize=20&search=Whey`
- **Authentication**: Bearer JWT
- **Role Required**: Any authenticated user
- **Response**: `200 OK` (List of inventory items with product details and quantity in stock).

### 5.2 Get Low-Stock Alerts
- **Method & Route**: `GET /api/inventory/low-stock`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Response**: `200 OK` (Items where `QuantityInStock <= ReorderThreshold`).

### 5.3 Adjust Stock Level (Atomic Restock / Deduction)
- **Method & Route**: `POST /api/inventory/adjust`
- **Authentication**: Bearer JWT
- **Role Required**: `Manager`, `Admin`
- **Request Body**:
```json
{
  "inventoryItemId": "b4a3c2d1-e5f6-7890-abcd-ef1234567890",
  "quantity": 25,
  "movementType": "Restock",
  "reason": "Supplier shipment received (PO-1042)"
}
```
- **Response**: `200 OK` (Returns updated stock quantity and logged audit record).
- **Errors**: `400 Bad Request` (Deduction exceeds available stock).

---

## 6. Class Scheduling & Booking (`/api/class-schedules` & `/api/bookings`)

### 6.1 List Upcoming Class Schedules
- **Method & Route**: `GET /api/class-schedules/upcoming?startDate=2026-10-01`
- **Authentication**: Bearer JWT
- **Role Required**: Any authenticated user
- **Response**: `200 OK` (Classes with trainer name, start/end time, capacity, and spots left).

### 6.2 Book Fitness Class
- **Method & Route**: `POST /api/bookings`
- **Authentication**: Bearer JWT
- **Role Required**: `Member`
- **Request Body**: `{"classScheduleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}`
- **Response**: `201 Created`
```json
{
  "bookingId": "c1d2e3f4-5678-90ab-cdef-1234567890ab",
  "classScheduleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Confirmed",
  "bookedAt": "2026-10-01T00:35:00Z"
}
```
- **Errors**: `400 Bad Request` (Class full / duplicate booking).

### 6.3 Cancel Booking
- **Method & Route**: `POST /api/bookings/{id}/cancel`
- **Authentication**: Bearer JWT
- **Role Required**: Member who booked or `Manager`/`Admin`
- **Response**: `200 OK` (Booking marked `Cancelled` and capacity freed).

---

## 7. Class Attendance (`/api/attendances`)

### 7.1 Record Member Attendance
- **Method & Route**: `POST /api/attendances/check-in`
- **Authentication**: Bearer JWT
- **Role Required**: `Trainer`, `Manager`, `Admin`
- **Request Body**:
```json
{
  "classScheduleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "memberId": "e43b1772-5bb9-4cfc-84cb-3bf996a6dfa2",
  "status": "Present"
}
```
- **Response**: `200 OK` (Attendance confirmed and recorded).

---

## 8. Memberships & Subscriptions (`/api/memberships`)

### 8.1 Get Active Member Subscription
- **Method & Route**: `GET /api/memberships/my`
- **Authentication**: Bearer JWT
- **Role Required**: `Member`
- **Response**: `200 OK` (Plan tier, start date, expiry date, and status).

### 8.2 Renew Membership
- **Method & Route**: `POST /api/memberships/renew`
- **Authentication**: Bearer JWT
- **Role Required**: `Member`, `Manager`, `Admin`
- **Request Body**:
```json
{
  "membershipPlanId": "7b8c9d0e-1f2a-3b4c-5d6e-7f8a9b0c1d2e",
  "durationMonths": 6
}
```
- **Response**: `200 OK` (Updated membership with new expiration date).

---

## 9. Notification Center (`/api/notifications`)

### 9.1 Get User Notifications
- **Method & Route**: `GET /api/notifications/my`
- **Authentication**: Bearer JWT
- **Role Required**: Any authenticated user
- **Response**: `200 OK` (Array of unread and historical notifications).

### 9.2 Mark Notification as Read
- **Method & Route**: `PUT /api/notifications/{id}/read`
- **Authentication**: Bearer JWT
- **Role Required**: Recipient user
- **Response**: `204 No Content`

---

## 10. System Diagnostics & Health (`/health` & `/api/system`)

### 10.1 Diagnostic Health Check
- **Method & Route**: `GET /health`
- **Authentication**: None (Public)
- **Role Required**: None
- **Response**: `200 OK`
```
Healthy
```

### 10.2 System Info & Metadata
- **Method & Route**: `GET /api/system/info`
- **Authentication**: None (Public)
- **Response**: `200 OK`
```json
{
  "serviceName": "SmartGym Core API",
  "version": "1.0.0",
  "environment": "Production",
  "databaseStatus": "Connected",
  "timestamp": "2026-10-01T00:36:00Z"
}
```
