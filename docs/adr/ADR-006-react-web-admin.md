# ADR-006: React 18 Web Admin Console with Modern Responsive CSS Design

## Status
Accepted

## Context
Administrators and Facility Managers require a desktop-optimized web console for supplement inventory oversight, CSV data exchange, class timetable scheduling, member moderation, and the crucial Human-in-the-Loop AI approval dashboard.

## Decision
We chose **React 18 with Vite and custom responsive CSS Design System**.

### Rationale:
- Rapid development cycle and hot module reloading with Vite.
- Clean component structure with custom CSS variables and glassmorphism aesthetic (rich, modern UI matching requirements).
- Centralized Axios/fetch HTTP client with automatic JWT token attachment and 401 redirect handling.
- Full parity with ASP.NET Core API endpoints.

## Consequences
- **Positive**: Rich desktop UX, snappy performance, clear component separation.
- **Negative**: Client-side state must be kept in sync with server status via targeted refetches.
