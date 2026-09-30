import os
import sys
import uuid
import json
import pytest
from pydantic import ValidationError

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.models.planner_models import (
    PlannerInput,
    PlannedStep,
    PlannerOutput,
    SupportedAgent,
    normalize_agent_name,
)
from smartgym_ai.agents.planner_agent import PlannerAgent
from smartgym_ai.services.llm.mock_client import MockLLMClient
from smartgym_ai.validation.schema_validator import SchemaValidator, MalformedJSONError
from smartgym_ai.models.workflow_models import WorkflowState, WorkflowStatus, WorkflowStartRequest
from smartgym_ai.services.state_store import PostgresWorkflowStateStore
from smartgym_ai.graph.workflow_engine import workflow_engine
import psycopg2


# --- 1. Normal Objective Test ---

@pytest.mark.asyncio
async def test_planner_normal_objective():
    mock_llm = MockLLMClient()
    agent = PlannerAgent(llm_client=mock_llm)

    planner_input = PlannerInput(
        workflow_id=uuid.uuid4(),
        facility_issue_id=uuid.uuid4(),
        objective="Perform full diagnostic inspection and repair for Treadmill Pro motor failure",
        request_summary="Treadmill motor smoking and emitting burning odor during high-speed workout session",
        equipment_name="Treadmill Pro 500"
    )

    plan = await agent.create_plan(planner_input)

    assert isinstance(plan, PlannerOutput)
    assert plan.plan_id is not None
    assert len(plan.steps) == 9
    assert plan.assigned_agent == SupportedAgent.SAFETY_VALIDATION.value
    assert len(plan.reason) > 5

    # Check that steps are sequential and properly assigned
    for idx, step in enumerate(plan.steps, start=1):
        assert step.step_order == idx
        assert len(step.step_name) >= 3
        assert len(step.reason) >= 5
        assert isinstance(step.approval_required, bool)
        # Verify assigned agent is in the supported agent enum
        assert step.assigned_agent in [a.value for a in SupportedAgent]

    # Verify key canonical step names exist in the plan
    step_names = [s.step_name.lower() for s in plan.steps]
    assert any("validate facility issue" in s for s in step_names)
    assert any("analyze equipment history" in s for s in step_names)
    assert any("check relevant inventory" in s for s in step_names)
    assert any("identify suitable supplier" in s for s in step_names)
    assert any("prepare repair proposal" in s for s in step_names)
    assert any("validate proposal" in s for s in step_names)
    assert any("request authorization" in s for s in step_names)
    assert any("execute approved action" in s for s in step_names)
    assert any("update ticket" in s for s in step_names)


# --- 2. Incomplete Objective Test ---

def test_planner_incomplete_objective():
    # Empty objective
    with pytest.raises(ValidationError) as exc_info:
        PlannerInput(
            facility_issue_id=uuid.uuid4(),
            objective="",
            request_summary="Detailed summary of issue"
        )
    assert "objective" in str(exc_info.value)

    # Whitespace-only objective
    with pytest.raises(ValidationError) as exc_info:
        PlannerInput(
            facility_issue_id=uuid.uuid4(),
            objective="     ",
            request_summary="Detailed summary of issue"
        )
    assert "objective" in str(exc_info.value)

    # Empty request summary
    with pytest.raises(ValidationError) as exc_info:
        PlannerInput(
            facility_issue_id=uuid.uuid4(),
            objective="Valid Objective Here",
            request_summary=""
        )
    assert "request_summary" in str(exc_info.value)


# --- 3. Malformed Output Test ---

@pytest.mark.asyncio
async def test_planner_malformed_output():
    malformed_json = "I plan to fix the treadmill by replacing the belt. Total steps: 3."
    valid_plan_json = json.dumps({
        "objective": "Treadmill repair plan",
        "plan_id": str(uuid.uuid4()),
        "steps": [
            {
                "step_order": 1,
                "step_name": "Validate facility issue",
                "assigned_agent": "SafetyValidationAgent",
                "reason": "Safety check",
                "approval_required": False
            },
            {
                "step_order": 2,
                "step_name": "Execute repair",
                "assigned_agent": "ActionExecutionAgent",
                "reason": "Execute maintenance",
                "approval_required": False
            }
        ],
        "assigned_agent": "SafetyValidationAgent",
        "reason": "Rapid triage and replacement plan",
        "approval_required": False
    })

    mock_llm = MockLLMClient()
    mock_llm.enqueue_response(malformed_json)
    mock_llm.enqueue_response(valid_plan_json)

    agent = PlannerAgent(llm_client=mock_llm)
    planner_input = PlannerInput(
        facility_issue_id=uuid.uuid4(),
        objective="Fix treadmill issue with recovery",
        request_summary="Belt slipped off motor pulley"
    )

    # Planner should recover after revision prompt retry
    plan = await agent.create_plan(planner_input)
    assert isinstance(plan, PlannerOutput)
    assert len(plan.steps) == 2
    assert plan.assigned_agent == "SafetyValidationAgent"


# --- 4. Invalid Step Test ---

def test_planner_invalid_step():
    # Empty step name
    with pytest.raises(ValidationError) as exc_info:
        PlannedStep(
            step_order=1,
            step_name="",
            assigned_agent="SafetyValidationAgent",
            reason="Valid reason"
        )
    assert "step_name" in str(exc_info.value)

    # Negative step order
    with pytest.raises(ValidationError) as exc_info:
        PlannedStep(
            step_order=0,
            step_name="Safety Check",
            assigned_agent="SafetyValidationAgent",
            reason="Valid reason"
        )
    assert "step_order" in str(exc_info.value)

    # Empty reason
    with pytest.raises(ValidationError) as exc_info:
        PlannedStep(
            step_order=1,
            step_name="Safety Check",
            assigned_agent="SafetyValidationAgent",
            reason="   "
        )
    assert "reason" in str(exc_info.value)

    # Non-sequential step orders in PlannerOutput
    step1 = PlannedStep(
        step_order=1,
        step_name="Safety Check",
        assigned_agent="SafetyValidationAgent",
        reason="Verify equipment"
    )
    step3 = PlannedStep(
        step_order=3,  # Invalid jump from 1 to 3
        step_name="Execute Repair",
        assigned_agent="ActionExecutionAgent",
        reason="Perform fix"
    )
    with pytest.raises(ValidationError) as exc_info:
        PlannerOutput(
            objective="Repair equipment",
            plan_id=uuid.uuid4(),
            steps=[step1, step3],
            assigned_agent="SafetyValidationAgent",
            reason="Test plan with invalid ordering",
            approval_required=False
        )
    assert "Invalid step order" in str(exc_info.value)


# --- 5. Unsupported Agent Assignment Test ---

def test_planner_unsupported_agent_assignment():
    # Random unapproved agent
    with pytest.raises(ValidationError) as exc_info:
        PlannedStep(
            step_order=1,
            step_name="Arbitrary Step",
            assigned_agent="UnauthorizedExternalChatbot",
            reason="Attempting to use an unsupported agent"
        )
    assert "Unsupported agent assignment" in str(exc_info.value)

    # Aliases normalize cleanly to canonical agents
    assert normalize_agent_name("Safety & Business Validation Agent") == SupportedAgent.SAFETY_VALIDATION.value
    assert normalize_agent_name("Gym Domain Analysis Agent") == SupportedAgent.DOMAIN_ANALYSIS.value
    assert normalize_agent_name("Action / Tool Agent") == SupportedAgent.ACTION_EXECUTION.value
    assert normalize_agent_name("inventory agent") == SupportedAgent.INVENTORY.value


# --- 6. Schema Validation & Restrictions Test ---

def test_planner_schema_validation_and_restrictions():
    # Attempting to bypass validation
    bypass_step = PlannedStep(
        step_order=1,
        step_name="Bypass validation for quick fix",
        assigned_agent="SafetyValidationAgent",
        reason="Skip compliance checks"
    )
    with pytest.raises(ValidationError) as exc_info:
        PlannerOutput(
            objective="Quick fix without checks",
            plan_id=uuid.uuid4(),
            steps=[bypass_step],
            assigned_agent="SafetyValidationAgent",
            reason="Shortcut maintenance",
            approval_required=False
        )
    assert "Planner restriction violated: Cannot bypass validation" in str(exc_info.value)

    # Serialization and deserialization roundtrip
    valid_step = PlannedStep(
        step_order=1,
        step_name="Validate facility issue",
        assigned_agent="SafetyValidationAgent",
        reason="Initial safety inspection",
        approval_required=False
    )
    plan = PlannerOutput(
        objective="Inspect treadmill",
        plan_id=uuid.uuid4(),
        steps=[valid_step],
        assigned_agent="SafetyValidationAgent",
        reason="Ensure member safety",
        approval_required=False
    )

    data = plan.model_dump()
    assert data["objective"] == "Inspect treadmill"
    assert len(data["steps"]) == 1

    restored = PlannerOutput.model_validate(data)
    assert restored.plan_id == plan.plan_id
    assert restored.steps[0].step_name == "Validate facility issue"


# --- 7. PostgreSQL Persistence Test ---

def test_planner_persistence_to_postgresql():
    """
    Verifies that Planner plans and planned steps are persisted into PostgreSQL
    'ai_workflows' and 'ai_workflow_steps' tables with referential integrity.
    """
    db_url = "postgresql://postgres:1234@localhost:5432/smartgym"

    # 1. Fetch an existing facility issue ID from PostgreSQL without an existing workflow
    with psycopg2.connect(db_url) as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT f."Id" FROM facility_issues f LEFT JOIN ai_workflows w ON f."Id" = w."IssueId" WHERE w."Id" IS NULL LIMIT 1')
            row = cur.fetchone()
            assert row is not None, "facility_issues table must have at least 1 record without an existing workflow"
            issue_id = uuid.UUID(str(row[0]))

    store = PostgresWorkflowStateStore(db_url=db_url)

    # 2. Construct workflow state with a structured plan
    workflow_id = uuid.uuid4()
    plan_id = uuid.uuid4()

    mock_plan = PlannerOutput(
        objective="Diagnose and repair motor bearing friction",
        plan_id=plan_id,
        steps=[
            PlannedStep(
                step_order=1,
                step_name="Validate facility issue",
                assigned_agent="SafetyValidationAgent",
                reason="Check physical hazard to members",
                approval_required=False
            ),
            PlannedStep(
                step_order=2,
                step_name="Analyze equipment history",
                assigned_agent="DomainAnalysisAgent",
                reason="Inspect historical runtime and telemetry",
                approval_required=False
            ),
            PlannedStep(
                step_order=3,
                step_name="Check relevant inventory",
                assigned_agent="InventoryAgent",
                reason="Check bearing stock",
                approval_required=False
            )
        ],
        assigned_agent="SafetyValidationAgent",
        reason="Standard triage for treadmill bearing friction",
        approval_required=False
    )

    state = WorkflowState(
        id=workflow_id,
        issue_id=issue_id,
        workflow_type="FacilityResolution",
        status=WorkflowStatus.Planning,
        current_step="Planning Complete",
        diagnosis_summary="Bearing friction detected on front drive roller",
        recommended_action="Replace front roller bearing assembly",
        estimated_confidence_score=0.92,
        requires_human_approval=False,
        structured_output={"plan": mock_plan.model_dump(), "plan_id": str(plan_id)}
    )

    # Add planned steps to workflow steps
    for p_step in mock_plan.steps:
        state.steps.append(
            from_planned_step_to_workflow_step(p_step)
        )

    try:
        # 3. Persist to PostgreSQL
        saved_state = store.create(state)
        assert saved_state.id == workflow_id

        # 4. Verify in PostgreSQL directly via SQL queries
        with psycopg2.connect(db_url) as conn:
            with conn.cursor() as cur:
                # Check ai_workflows table
                cur.execute('SELECT "Id", "CurrentStep", "StructuredOutputPayloadJson" FROM ai_workflows WHERE "Id" = %s', (str(workflow_id),))
                wf_row = cur.fetchone()
                assert wf_row is not None
                assert wf_row[0] == str(workflow_id)
                assert wf_row[1] == "Planning Complete"

                payload = wf_row[2]
                if isinstance(payload, str):
                    payload = json.loads(payload)
                assert "plan" in payload
                assert payload["plan_id"] == str(plan_id)

                # Check ai_workflow_steps table
                cur.execute('SELECT "StepName", "StepOrder", "Summary" FROM ai_workflow_steps WHERE "WorkflowId" = %s ORDER BY "StepOrder" ASC', (str(workflow_id),))
                step_rows = cur.fetchall()
                assert len(step_rows) == 3
                assert step_rows[0][0] == "Validate facility issue"
                assert step_rows[0][1] == 1
                assert step_rows[1][0] == "Analyze equipment history"
                assert step_rows[1][1] == 2
                assert step_rows[2][0] == "Check relevant inventory"
                assert step_rows[2][1] == 3

        # 5. Verify retrieval through store.get
        retrieved = store.get(workflow_id)
        assert retrieved is not None
        assert retrieved.id == workflow_id
        assert len(retrieved.steps) == 3
        assert retrieved.steps[0].step_name == "Validate facility issue"
    finally:
        with psycopg2.connect(db_url) as conn:
            with conn.cursor() as cur:
                cur.execute('DELETE FROM ai_workflow_steps WHERE "WorkflowId" = %s', (str(workflow_id),))
                cur.execute('DELETE FROM ai_workflows WHERE "Id" = %s', (str(workflow_id),))
            conn.commit()


def from_planned_step_to_workflow_step(planned: PlannedStep):
    from smartgym_ai.models.workflow_models import WorkflowStepState, WorkflowStepStatus
    return WorkflowStepState(
        id=uuid.uuid4(),
        step_name=planned.step_name,
        step_order=planned.step_order,
        status=WorkflowStepStatus.Pending,
        summary=f"Delegated to {planned.assigned_agent}: {planned.reason}"
    )


# --- 8. LangGraph Workflow Next Agent / Step Continuation Test ---

@pytest.mark.asyncio
async def test_langgraph_workflow_knows_next_agent_and_step():
    """
    Verifies that the LangGraph workflow engine executes the PLANNER node,
    populates structured plan details, identifies the next agent and step,
    and enables continuation to subsequent specialized agents.
    """
    db_url = "postgresql://postgres:1234@localhost:5432/smartgym"
    test_issue_id = uuid.uuid4()
    test_equipment_id = uuid.uuid4()
    try:
        with psycopg2.connect(db_url) as conn:
            with conn.cursor() as cur:
                cur.execute('SELECT f."Id", f."EquipmentId" FROM facility_issues f LEFT JOIN ai_workflows w ON f."Id" = w."IssueId" WHERE w."Id" IS NULL AND f."EquipmentId" IS NOT NULL LIMIT 1')
                row = cur.fetchone()
                if row:
                    test_issue_id = uuid.UUID(str(row[0]))
                    if row[1]:
                        test_equipment_id = uuid.UUID(str(row[1]))
    except Exception:
        pass

    request = WorkflowStartRequest(
        issue_id=test_issue_id,
        equipment_id=test_equipment_id,
        issue_title="Incline Motor Malfunction",
        description="Incline motor stalled at 12% grade; buzzing noise detected.",
        equipment_name="Treadmill Elevation T95",
        workflow_type="FacilityResolution"
    )

    try:
        state = await workflow_engine.start_workflow(request)

        # Workflow produced a structured plan
        assert state.structured_output is not None
        assert "plan" in state.structured_output

        plan_data = state.structured_output["plan"]
        assert "plan_id" in plan_data
        assert "steps" in plan_data
        assert len(plan_data["steps"]) >= 1

        # Workflow knows next agent and next step
        assert "next_agent" in state.structured_output
        assert plan_data["assigned_agent"] == "SafetyValidationAgent"
        assert plan_data["steps"][0]["step_name"] == "Validate facility issue"

        # Steps are populated on the workflow state
        step_names = [s.step_name for s in state.steps]
        assert "Validate facility issue" in step_names
        assert "Remediation Complete" in step_names
    finally:
        try:
            with psycopg2.connect(db_url) as conn:
                with conn.cursor() as cur:
                    cur.execute('DELETE FROM ai_workflow_steps WHERE "WorkflowId" = %s', (str(state.id),))
                    cur.execute('DELETE FROM ai_workflows WHERE "Id" = %s', (str(state.id),))
                conn.commit()
        except Exception:
            pass
