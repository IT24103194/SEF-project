# ADR-004: Multi-Agent Orchestration with Python, FastAPI & LangGraph

## Status
Accepted

## Context
The SE3090 Assignment 1 and SmartGym Master Specification mandate a genuine multi-agent Agentic AI workflow rather than a generic prompt-response chatbot. The AI workflow must handle complex equipment breakdown reports by coordinating multiple specialized agents, making conditional routing decisions, using allow-listed tools, and providing persistent state checkpoints.

## Options Considered
1. **Direct OpenAI/LLM API Calls with Function Calling**: Simple, but lacks explicit state graph visualization, deterministic validation steps, and structured multi-agent coordination.
2. **AutoGen**: Conversational agent framework, but conversational turns can produce non-deterministic loops and lack explicit workflow graph controls.
3. **LangGraph (LangChain ecosystem) + FastAPI**: Graph-based state machine where each node represents a specialized agent, edges represent deterministic or conditional transitions, and state is explicitly checkpointed.

## Decision
We chose **Python 3.12/3.13, FastAPI, and LangGraph**.

### Architecture:
1. **Planner Agent (Coordinator)**: Analyzes the facility issue, breaks down the remediation plan into structured phases, and coordinates sub-agents.
2. **Safety & Business Validation Agent**: Performs deterministic safety checks, verifies gym operational protocols, and enforces gym warranty and supplier compliance.
3. **Gym Domain Analysis Agent**: Diagnoses mechanical/electronic equipment failure modes, identifies required spare parts, and calculates labor and parts costs.
4. **Action / Tool Agent**: Interacts with external supplier catalogs, drafts repair orders, and dispatches transactional emails upon approval.

## Consequences
- **Positive**: Clear visual state graph, deterministic guardrail validation before tool execution, native support for state interruptions (Human-in-the-Loop), easily mockable for offline testing and viva demonstrations.
- **Negative**: Adds a Python runtime dependency alongside the .NET backend.
