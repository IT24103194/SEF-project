# SmartGym Testing Strategy & Verification Plan

## 1. Multi-Tier Testing Pyramid
Adhering to **Section 18 (Testing Strategy)** of the Master Specification:

1. **Backend Unit & Integration Tests (xUnit + WebApplicationFactory)**:
   - Location: `tests/backend/SmartGym.Api.Tests`
   - Covers: Business rules (booking capacity limits, inventory adjustments, password hashing, JWT claims, role-based authorization, RFC 7807 error responses).

2. **AI Microservice Unit & Guardrail Tests (pytest + httpx)**:
   - Location: `tests/ai/`
   - Covers: 4-agent state transitions, prompt injection defenses, deterministic safety validator rules, HITL cost threshold triggers.

3. **Frontend Component & Integration Tests (Vitest / React Testing Library)**:
   - Location: `tests/frontend/`
   - Covers: Redux state management, component rendering, form validation, error alert displays.

4. **Mobile Client Tests (flutter_test)**:
   - Location: `mobile/smartgym_mobile/test/`
   - Covers: Widget pump tests, Riverpod state propagation, secure storage mocking.

5. **End-to-End Workflow Testing**:
   - Location: `tests/e2e/`
   - Verifies the cross-platform flow: Member reports broken equipment in mobile -> AI calculates repair cost > Rs. 25,000 -> Manager approves in React -> Status reflects in mobile.

6. **Performance & Load Testing (k6)**:
   - Location: `tests/performance/`
   - Target: API response times under 200ms at 50 concurrent requests.
