# ADR-001: React State Management Architecture

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: Full-Stack Architecture Team  
**Consulted**: Senior Frontend & DevOps Engineers  

---

## 1. Context & Problem Statement
The SmartGym Web Admin Console (`smartgym-web`) requires predictable, scalable state management across diverse operational domains:
- Authentication credentials and active role metadata.
- Real-time Human-in-the-Loop (HITL) approval queues with optimistic updates.
- Supplement inventory catalog with atomic stock adjustments.
- Facility maintenance ticket tracking with multi-stage progress updates.

We need a unified frontend state management solution that avoids prop-drilling, simplifies async API integration, and supports isolated unit testing.

---

## 2. Options Considered
1. **Redux Toolkit (RTK)**: Standard opinionated Redux with `@reduxjs/toolkit` and `createSlice`.
2. **React Context API + `useReducer`**: Built-in React state primitives without external dependencies.
3. **Zustand**: Lightweight, hook-based un-opinionated store.
4. **TanStack Query (React Query) + Local State**: Server-state caching library paired with local component hooks.

---

## 3. Decision
We chose **Option 1: Redux Toolkit (RTK)**.
We organized the store into dedicated domain slices:
- `authSlice`: User identity, access tokens, and role permissions.
- `approvalSlice`: Pending HITL approval tickets and decision actions.
- `facilitySlice`: Equipment issue registry and resolution timelines.
- `inventorySlice`: Supplement catalog, low-stock thresholds, and adjustment modal state.

---

## 4. Consequences
### Positive Consequences:
- **Predictable State Mutations**: Redux Immer integration enables safe immutable state updates.
- **Centralized DevTools Debugging**: Action dispatch tracking and state diff inspection.
- **Testability**: Slices and reducers are pure functions easily tested using Vitest and `@testing-library/react`.
- **Ecosystem Standard**: Established patterns familiar to academic evaluators and enterprise teams.

### Negative Consequences:
- Boilerplate setup compared to Zustand or basic Context.
- Requires wrapping the application tree in `<Provider store={store}>`.

---

## 5. Rejected Alternatives
- **Context API + `useReducer`**: Rejected due to re-render performance bottlenecks across large component trees when frequently updating approval lists.
- **Zustand**: Rejected to maintain explicit action type conventions and strict Redux architectural standards expected in university evaluation.
- **React Query**: Rejected to prevent dual state systems (server vs. client) for our synchronous UI dialogs and approval queues.
