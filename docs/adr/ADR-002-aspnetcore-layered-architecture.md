# ADR-002: ASP.NET Core 8 Layered Architecture & Authoritative Backend

## Status
Accepted

## Context
SmartGym requires robust security, deterministic validation, and strict enforcement of business rules across member management, inventory controls, class bookings, and facility resolutions. Frontends (React and Flutter) must not directly query databases, bypass business validations, or directly trigger external email or AI endpoints.

## Options Considered
1. **Direct Database Access via BaaS (e.g. Supabase/Firebase)**: Frontend connects directly to database or cloud functions.
2. **Minimal APIs**: ASP.NET Core minimal endpoints in a single file.
3. **Controller-Service-Repository Layered Architecture**: Explicit separation of Controllers (HTTP & routing), Services (business rules), Data/DbContext (persistence), and DTOs (data contracts).

## Decision
We chose the **Controller-Service-Repository Layered Architecture in ASP.NET Core 8 Web API**.

### Rationale:
- ASP.NET Core acts as the authoritative public application gateway.
- Enforces server-side validation using standard DataAnnotations and domain services before data touches PostgreSQL.
- Eliminates direct frontend access to internal AI microservices and third-party APIs.
- Provides standard RFC 7807 ProblemDetails error responses, built-in dependency injection, and automatic Swagger documentation.

## Consequences
- **Positive**: High testability via xUnit, clean separation of concerns, explicit authorization attributes (`[Authorize(Roles = "Admin")]`), maintainable code for academic viva defense.
- **Negative**: Requires explicit DTO mapping between HTTP requests, domain entities, and responses.
