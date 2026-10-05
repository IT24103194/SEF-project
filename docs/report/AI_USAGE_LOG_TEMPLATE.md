# Academic AI Usage Log Template (SE3090)

According to university academic integrity guidelines, any use of Generative AI tools (e.g. Google Gemini, Claude, ChatGPT, GitHub Copilot) must be documented transparently. This template records all AI assistance utilized during development.

---

## AI Usage Log Entry Format

| Field | Description / Value |
| :--- | :--- |
| **Log ID** | `AI-LOG-YYYYMMDD-###` |
| **Date & Time** | ISO 8601 Timestamp (e.g. `2026-09-28T14:30:00Z`) |
| **Student Registration ID** | Student ID (e.g. `IT24103194`) |
| **AI Tool Used** | Specific tool (e.g. `Google Antigravity IDE`, `Claude Code`, `GitHub Copilot`) |
| **Model Version** | Exact model identifier (e.g. `gemini-1.5-pro`, `claude-3-5-sonnet`) |
| **Target Component** | Backend / Database / React / Flutter / Agentic AI / Tests |
| **Prompt / Task Description** | Verbatim prompt or task objective given to the AI tool |
| **Generated Output Summary** | Summary of the code, test, or documentation drafted by the AI |
| **Manual Changes Made** | Modifications, corrections, refactorings, or security hardening applied by the student |
| **Rejected Output** | Parts of the AI suggestions that were rejected (e.g. invalid syntax, hallucinations, security anti-patterns) |
| **Verification Method** | How correctness was verified (e.g. `dotnet test`, `flutter test`, manual debugging, database query inspection) |
| **Commit / Test Reference** | Git commit hash (e.g. `8205e4b`) or specific test method name (e.g. `AuthApiTests.cs:Login_ValidCredentials`) |

---

## Example Log Entries

### Entry 1: Backend DTO & Validator Implementation
- **Log ID**: `AI-LOG-20260928-001`
- **Date & Time**: `2026-09-28T10:15:00Z`
- **Student ID**: `IT24103194`
- **AI Tool**: `Google Antigravity IDE`
- **Model**: `gemini-1.5-pro`
- **Target Component**: `SmartGym.Api / DTOs / Inventory`
- **Prompt**: "Generate a FluentValidation validator for InventoryAdjustmentRequest requiring positive quantity and valid movement type enum."
- **Generated Output Summary**: Generated `InventoryAdjustmentRequestValidator` class inheriting from `AbstractValidator<InventoryAdjustmentRequest>`.
- **Manual Changes Made**: Adjusted enum parsing logic to match `StockMovementType.Restock` instead of string literals.
- **Rejected Output**: Rejected suggestion to catch generic `Exception` inside the validator rule.
- **Verification Method**: Ran `dotnet test tests/backend/SmartGym.Api.Tests` (`InventoryApiTests.cs`).
- **Commit Reference**: `Commit 8205e4b` / `SmartGym.Api.Tests.dll`

### Entry 2: LangGraph State Machine Node Definition
- **Log ID**: `AI-LOG-20260929-004`
- **Date & Time**: `2026-09-29T16:45:00Z`
- **Student ID**: `IT24103194`
- **AI Tool**: `Google Antigravity IDE`
- **Model**: `gemini-1.5-pro`
- **Target Component**: `ai-service / smartgym_ai / graph / workflow_engine.py`
- **Prompt**: "Draft the LangGraph conditional routing logic for _approval_gate_node to pause execution if requires_human_approval is true."
- **Generated Output Summary**: Provided `StateGraph` conditional edge configuration with status routing to `paused`, `rejected`, or `execution`.
- **Manual Changes Made**: Wrapped the approval check in a deterministic database verification call to PostgreSQL to prevent approval bypass.
- **Rejected Output**: Rejected suggestion to use an in-memory dictionary without database persistence for approvals.
- **Verification Method**: Executed `pytest tests/ai/test_complete_workflow.py` (Golden Case 3 & 4).
- **Commit Reference**: `Commit c68b39a`

### Entry 3: Facility Issue Emergency Escalation and Safety Validation
- **Log ID**: `AI-LOG-20261005-001`
- **Date & Time**: `2026-10-05T19:30:00Z`
- **Student ID**: `IT24103362`
- **AI Tool**: `Google Antigravity IDE`
- **Model**: `gemini-3.8-flash`
- **Target Component**: `backend / SmartGym.Api / DTOs / Facility & SafetyValidator`
- **Prompt**: "Design urgency level contracts, SLA tracking logic, and prompt injection leetspeak detection rules for facility issues."
- **Generated Output Summary**: Provided DTO schema additions and regex patterns for obfuscated injection detection.
- **Manual Changes Made**: Tuned SLA thresholds to reflect gym maintenance contracts (4h for Critical, 12h for High) and added regex bounds to prevent false positives.
- **Rejected Output**: Rejected loose regex that flagged common fitness terms like 'press' or 'drop'.
- **Verification Method**: Verified via `dotnet build backend/SmartGym.Api` and unit test assertions.
- **Commit Reference**: `IT24103362 branch commit series`

