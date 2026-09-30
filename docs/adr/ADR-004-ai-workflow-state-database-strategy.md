# ADR-004: AI Workflow-State Database & Persistence Strategy

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: Data & AI Engineering Team  
**Consulted**: Database Administrator & Backend Lead  

---

## 1. Context & Problem Statement
When a member submits a facility issue, the multi-agent AI pipeline runs through multiple steps over seconds or hours (in cases requiring human manager approval). The system must persist:
- The overall workflow execution state (`Initiated`, `Planning`, `Executing`, `AwaitingApproval`, `Completed`, `Failed`).
- Chronological step executions with assigned agents and runtimes.
- Tool invocation parameters, outputs, and execution latencies.
- Deterministic validation results and safety violation reports.
- Human-in-the-Loop decision logs.

We need a database persistence strategy that guarantees ACID integrity, enables cross-tier querying from ASP.NET Core, and provides complete auditability.

---

## 2. Options Considered
1. **Dedicated PostgreSQL Relational Tables (Normalized Model)**: Store workflow state, steps, tool records, and validations in dedicated relational tables (`ai_workflows`, `ai_workflow_steps`, `ai_tool_executions`, `ai_validation_results`, `approvals`).
2. **NoSQL Document Database (MongoDB / DynamoDB)**: Store the entire workflow state as a serialized JSON blob in a document collection.
3. **In-Memory Store (Redis Only)**: Keep state exclusively in Redis cache with TTL.
4. **Single JSONB Column in `facility_issues`**: Dump the execution state into a PostgreSQL JSONB column on the existing ticket table.

---

## 3. Decision
We chose **Option 1: Dedicated PostgreSQL Relational Tables** paired with an in-memory runtime cache for hot execution.
- Relational schema:
  - `ai_workflows`: High-level workflow header linked to `facility_issues`.
  - `ai_workflow_steps`: 1-to-many child records tracking individual agent actions.
  - `ai_tool_executions`: Granular tool execution audit table with input/output payloads and execution latencies.
  - `ai_validation_results`: Records of deterministic rule checks and prompt injection scans.
  - `approvals`: Formal approval records linked to the approving manager's user ID.

---

## 4. Consequences
### Positive Consequences:
- **Relational Integrity**: Foreign key constraints guarantee workflow records are cleaned up or retained consistently with the underlying facility issue.
- **Cross-Tier Visibility**: ASP.NET Core Web API queries workflow and approval status directly via Entity Framework Core without translating NoSQL schemas.
- **Strict Auditing**: Every tool call and approval decision is queryable by timestamp, user ID, and correlation ID for compliance.
- **Sub-Millisecond Read Latency**: Proper indexing yields < 1ms retrieval for active workflows.

### Negative Consequences:
- Requires defining Entity Framework Core entity classes and running migrations (`Add-Migration AIWorkflowTables`).

---

## 5. Rejected Alternatives
- **MongoDB / DynamoDB**: Rejected to avoid introducing a second database technology stack into the monorepo, which would increase DevOps overhead and break transactional boundaries with PostgreSQL.
- **Redis Only**: Rejected because workflow history and approval audits must be durable across server restarts and long approval delays.
- **Single JSONB Column**: Rejected because querying tool failures or pending approvals across thousands of tickets would require expensive JSON scans and lack foreign key enforcement.
