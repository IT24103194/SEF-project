# ADR-005: Human-in-the-Loop (HITL) Financial Threshold Gate for Repair Orders

## Status
Accepted

## Context
Autonomous AI agents must not have unchecked authority to commit significant financial expenditure or trigger legally binding orders to suppliers. Under the SmartGym Master Specification, facility repair estimations exceeding a predefined financial threshold (default: Rs. 25,000 / $250) must require explicit human authorization before execution.

## Options Considered
1. **Fully Autonomous Execution**: Agent calculates costs and automatically dispatches purchase orders to suppliers. (Rejected: High financial risk, hallucination risk, violates safety policies).
2. **Post-Action Notification**: Agent places order immediately and notifies manager afterward. (Rejected: Cannot prevent unwarranted expenditures).
3. **Stateful Human-in-the-Loop (HITL) Interruption Gate**: The agentic workflow calculates cost estimates, compares against `AI_APPROVAL_COST_THRESHOLD`, and if the threshold is exceeded, transitions to `RequiresApproval` status. Execution pauses, saving full state to the database, until an authorized Facility Manager explicitly approves or rejects via the React Admin Console.

## Decision
We chose the **Stateful Human-in-the-Loop (HITL) Interruption Gate**.

### Workflow:
1. Member reports broken equipment in Flutter app.
2. AI Service analyzes issue, estimates parts & labor cost.
3. If cost > Rs. 25,000, status becomes `RequiresApproval`.
4. Facility Manager logs into React Web Admin, reviews AI diagnosis, cost breakdown, and suggested supplier.
5. If Approved: AI Action Agent dispatches supplier request email, generates `RepairOrder`, and updates Flutter status.
6. If Rejected: Workflow terminates with reason logged, notifying the member.

## Consequences
- **Positive**: Strict financial safety, robust academic demonstration of real-world AI governance, auditable approval log.
- **Negative**: Adds state machine complexity requiring persistent paused workflow state.
