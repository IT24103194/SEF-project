# ADR-003: Agentic AI Framework & Multi-Agent Orchestration

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: AI Architecture & Engineering Team  
**Consulted**: Senior AI Engineer & Systems Architect  

---

## 1. Context & Problem Statement
SmartGym requires an autonomous multi-agent pipeline to triage and resolve high-impact facility maintenance issues. The system must coordinate 4 distinct agents:
1. `SafetyValidationAgent`: Content moderation, prompt injection detection, and policy checks.
2. `PlannerAgent`: Task decomposition and dependency delegation.
3. `DomainAnalysisAgent`: Equipment diagnosis, inventory check, and cost estimation.
4. `ActionExecutionAgent`: Repair order drafting, vendor RFQ dispatch, and status updates.

Crucially, the architecture must support:
- Cyclical and conditional graph transitions.
- Stateful checkpointing and Human-in-the-Loop (HITL) execution pauses when estimated costs exceed the financial threshold (LKR 10,000 / LKR 25,000).
- Deterministic guardrails and schema validation.

---

## 2. Options Considered
1. **LangGraph (`StateGraph`)**: Graph-based state machine framework for Python with native conditional routing, checkpointing, and HITL interruption.
2. **CrewAI**: Role-playing autonomous agent framework based on sequential/hierarchical processes.
3. **AutoGen (Microsoft)**: Multi-agent conversational framework based on conversational turns.
4. **Custom Procedural Script (Async Python)**: Hardcoded `if/else` procedural coordinator.

---

## 3. Decision
We chose **Option 1: LangGraph (`langgraph`)**.
We defined a typed `WorkflowGraphState` and constructed an explicit state graph:
- Entry point: `_initialize_node` → `planner`
- Conditional routing: `_route_after_planner` evaluates whether to route to `safety_validation` or `safe_failure`.
- Gate routing: `_approval_gate_node` pauses the graph when `requires_human_approval == True` and resumes execution only upon receiving human manager approval.

---

## 4. Consequences
### Positive Consequences:
- **Explicit State Transitions**: Eliminates unpredictable LLM conversational loops; each transition is an explicit graph edge.
- **Native Human-in-the-Loop**: Execution safely pauses and resumes without losing workflow context.
- **Structured Schema Enforcers**: Seamlessly pairs with Pydantic v2 schemas to guarantee typed tool inputs and outputs.
- **Observability**: Every node execution emits structured correlation logs (`ai_logger`) and persists tool timing in PostgreSQL.

### Negative Consequences:
- Requires Python 3.11+ async runtime and specialized dependency stack (`langgraph`, `pydantic`).

---

## 5. Rejected Alternatives
- **CrewAI**: Rejected because agent collaboration is based on unstructured text prompts and freeform conversational consensus, which creates non-deterministic failure modes unacceptable in financial and safety workflows.
- **AutoGen**: Rejected because conversational agents struggle with strict programmatic financial approval gates and exact schema validation.
- **Custom Procedural Script**: Rejected because hardcoded loops lack re-entrancy, checkpointing, and clean graph visualization required by academic assessment.
