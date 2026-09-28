# SmartGym PostgreSQL Database Architecture & Schema Strategy

## 1. Overview
The persistence layer utilizes **PostgreSQL 16** managed through **Entity Framework Core 8** and **Npgsql.EntityFrameworkCore.PostgreSQL**.

## 2. Schema Modularization
The database is structured across the 4 core business domains plus cross-cutting authentication, AI audit, and notification tables:
- **Authentication & User Profiles**: `Users`, `Roles`, `UserRoles`, `RefreshTokens`
- **Component 1 (Inventory)**: `Suppliers`, `Categories`, `Products`, `InventoryItems`, `InventoryAdjustments`, `PurchaseOrders`, `PurchaseOrderItems`
- **Component 2 (Facility Resolution & AI)**: `Equipment`, `EquipmentMaintenanceLogs`, `Feedback`, `FacilityIssues`, `AiWorkflows`, `AiWorkflowSteps`, `RepairOrders`, `RepairOrderParts`
- **Component 3 (Class Scheduling & Booking)**: `FitnessClasses`, `ClassCategories`, `Trainers`, `TrainerAvailabilities`, `ClassSchedules`, `ClassBookings`, `ClassAttendances`
- **Component 4 (Membership & Goal Tracking)**: `MembershipPlans`, `Memberships`, `MembershipPayments`, `FitnessGoals`, `ProgressLogs`, `Milestones`
- **Cross-Cutting**: `Notifications`, `AuditLogs`

## 3. Design Principles
1. **UTC Timestamps**: All temporal columns use `timestamp with time zone` (`timestamptz`).
2. **Cascades & Referential Integrity**: Foreign keys enforce referential integrity with explicit cascade delete or restrict rules.
3. **JSONB Auditing**: The `AiWorkflowSteps` table utilizes PostgreSQL JSONB for flexible agent observation logs.
