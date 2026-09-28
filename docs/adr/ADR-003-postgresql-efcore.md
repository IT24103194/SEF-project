# ADR-003: PostgreSQL 16 & Entity Framework Core Persistence Strategy

## Status
Accepted

## Context
SmartGym requires a production-grade relational database to model complex domain relationships spanning gym members, fitness goals, trainer scheduling, class reservations, supplement stock, equipment assets, and AI repair workflow states.

## Options Considered
1. **NoSQL / Document Store (MongoDB)**: Flexible documents, but poor relational integrity and difficult constraint enforcement for class capacity and financial transactions.
2. **SQLite**: Embedded database, suitable for lightweight testing, but lacks enterprise concurrency, enum types, and production deployment parity.
3. **PostgreSQL 16 with Entity Framework Core (Npgsql)**: Enterprise open-source relational database with strong ACID guarantees, foreign keys, cascade rules, indexing, and UTC timestamp handling.

## Decision
We chose **PostgreSQL 16 with Entity Framework Core 8 (Npgsql)**.

### Rationale:
- Strongly typed C# entity mapping with Fluent API configuration.
- Native support for automated schema migrations via `dotnet ef migrations`.
- Robust foreign key constraints preventing orphan records (e.g. bookings referencing deleted schedules).
- UTC timezone standardization using `timestamp with time zone` (`timestamptz`).
- Native JSONB capabilities for storing flexible AI agent audit logs and execution steps.

## Consequences
- **Positive**: Strict data integrity, repeatable schema evolution, production parity between local Docker and cloud hosting.
- **Negative**: Requires careful migration management when modifying foreign keys or column types.
