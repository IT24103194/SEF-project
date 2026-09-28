# SmartGym Agentic AI Evaluation & Safety Framework

## 1. Multi-Agent Evaluation Architecture
The AI subsystem coordinates 4 distinct specialized agents using LangGraph:
- **Planner Agent**: Decomposes incident reports into triage plans.
- **Safety & Business Validation Agent**: Enforces warranty terms, safety regulations, and gym compliance rules deterministically.
- **Gym Domain Analysis Agent**: Identifies equipment breakdown symptoms, diagnoses part failures, and computes labor and component costs.
- **Action / Tool Agent**: Dispatches authorized supplier orders and notifications via allow-listed tools.

## 2. Safety Guardrails & Human-in-the-Loop (HITL) Gate
- **Threshold Rule**: Any repair with estimated cost exceeding `AI_APPROVAL_COST_THRESHOLD` (default: **Rs. 25,000.00**) MUST transition to `RequiresApproval` status.
- **Prompt Injection Defense**: Input validation sanitizes user problem descriptions before passing to LLM prompts, preventing instruction overriding.
- **Deterministic Validation**: Financial calculations and safety compliance checks are performed using deterministic Python code rather than generative LLM completions.

## 3. Evaluation Metrics
- **Plan Accuracy**: Successful identification of correct equipment failure mode (> 90%).
- **Safety Compliance**: 100% adherence to safety shutdown rules when severe electrical/mechanical danger is detected.
- **HITL Enforcement**: 0 unapproved expenditures exceeding Rs. 25,000 threshold.
