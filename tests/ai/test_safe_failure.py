import os
import sys
import uuid
import pytest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.services.resilience import SafeFailureHandler
from smartgym_ai.models.workflow_models import WorkflowState, WorkflowStatus, WorkflowStepStatus


def test_safe_failure_handler_creates_audited_failed_state():
    state = WorkflowState(
        id=uuid.uuid4(),
        issue_id=uuid.uuid4(),
        status=WorkflowStatus.Running,
        current_step="evaluate_diagnosis"
    )

    error = TimeoutError("External provider timed out after 30 seconds.")
    failed_state = SafeFailureHandler.handle_safe_failure(
        state=state,
        error=error,
        step_name="evaluate_diagnosis",
        fallback_action="Notify staff maintenance team for manual inspection"
    )

    assert failed_state.status == WorkflowStatus.Failed
    assert "TimeoutError" in (failed_state.error_details or "")
    assert len(failed_state.steps) == 1
    assert failed_state.steps[0].step_name == "evaluate_diagnosis"
    assert failed_state.steps[0].status == WorkflowStepStatus.Failed
    assert failed_state.recommended_action == "Notify staff maintenance team for manual inspection"
