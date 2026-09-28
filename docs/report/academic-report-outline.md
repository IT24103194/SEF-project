# SmartGym Academic Report Outline (SE3090 Assignment 1)

## 1. Executive Summary
- Problem statement: Fragmentation of gym management, equipment failure resolution, and supplier coordination.
- Solution: Unified polyglot ecosystem with authoritative ASP.NET Core backend, PostgreSQL persistence, React web administration, Flutter mobile client, and LangGraph multi-agent AI.

## 2. System Architecture & Tech Stack Justification
- Monorepo design and single authoritative backend gateway.
- Technology selection rationale (.NET 8, PostgreSQL, React 18, Flutter 3, LangGraph).

## 3. Four Core Functional Components & Student Ownership
- Student 1: Supplier & Supplement Inventory Management (Gym Domain Analysis Agent contribution)
- Student 2: Feedback & Facility Resolution (Safety & Business Validation Agent contribution)
- Student 3: Class Scheduling & Booking (Coordinator / Planner Agent contribution)
- Student 4: Membership & Goal Tracking (Action / Tool Agent contribution)

## 4. Agentic AI & Human-in-the-Loop Implementation
- LangGraph state machine with 4 specialized agents.
- HITL financial approval gate (Rs. 25,000 threshold).

## 5. Security & Error Handling
- JWT Authentication and Role-Based Access Control (RBAC).
- RFC 7807 ProblemDetails error handling.
- Input validation and prompt injection defenses.

## 6. Testing, CI/CD & Verification Results
- Unit and integration testing results across all tiers.
- GitHub Actions CI pipeline summary.

## 7. Individual Contributions & Reflections
- Technical breakdown of each student's contributions across backend, frontend, database, mobile, and AI.
