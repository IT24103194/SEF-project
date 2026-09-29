import os
import sys
import uuid
import pytest
from fastapi.testclient import TestClient

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.main import app
from smartgym_ai.configuration.settings import settings
from smartgym_ai.graph.workflow_engine import workflow_engine

client = TestClient(app)

AUTH_HEADERS = {
    "X-Internal-Api-Key": settings.AI_SERVICE_API_KEY,
    "X-Correlation-ID": "test-integration-correlation-id"
}


def test_internal_workflow_endpoints_require_api_key():
    # Attempt to start workflow without API key header
    res = client.post(
        "/internal/workflows/start",
        json={
            "issue_id": str(uuid.uuid4()),
            "issue_title": "Unauthorized test",
            "description": "Testing missing auth key",
            "equipment_name": "Treadmill"
        }
    )
    assert res.status_code == 401
    assert "Unauthorized" in res.json().get("detail", "")


def test_start_workflow_with_low_cost_completes():
    # Mock LLM default returns estimated_cost = 15000.0 (< 25000.0 threshold)
    test_issue_id = str(uuid.uuid4())
    res = client.post(
        "/internal/workflows/start",
        headers=AUTH_HEADERS,
        json={
            "issue_id": test_issue_id,
            "issue_title": "Minor belt squeak needing lubrication",
            "description": "Belt makes slight squeak when starting up",
            "equipment_name": "Elliptical",
            "workflow_type": "FacilityResolution"
        }
    )
    assert res.status_code == 201
    data = res.json()
    assert "id" in data
    assert data["status"] == "Completed"
    assert data["requires_human_approval"] is False
    assert res.headers.get("X-Correlation-ID") == "test-integration-correlation-id"


def test_start_workflow_with_high_cost_pauses_for_approval():
    # Enqueue an LLM response with estimated_cost >= 25,000 LKR
    high_cost_diagnosis = {
        "diagnosis_summary": "Major motor controller board blowout",
        "recommended_action": "Replace motor controller assembly and re-calibrate",
        "estimated_cost": 38000.0,
        "confidence_score": 0.94
    }
    workflow_engine.llm_client.enqueue_json_response(high_cost_diagnosis)

    test_issue_id = str(uuid.uuid4())
    res = client.post(
        "/internal/workflows/start",
        headers=AUTH_HEADERS,
        json={
            "issue_id": test_issue_id,
            "issue_title": "Severe motor failure",
            "description": "Smoke and burning smell from treadmill motor base",
            "equipment_name": "Treadmill Pro",
            "workflow_type": "FacilityResolution"
        }
    )
    assert res.status_code == 201
    data = res.json()
    wf_id = data["id"]
    assert data["status"] == "AwaitingApproval"
    assert data["requires_human_approval"] is True
    assert data["estimated_cost"] == 38000.0

    # Resume the workflow with approval
    resume_res = client.post(
        f"/internal/workflows/{wf_id}/resume",
        headers=AUTH_HEADERS,
        json={
            "action": "approve",
            "comments": "Cost verified and approved by Gym Operations Manager"
        }
    )
    assert resume_res.status_code == 200
    resumed_data = resume_res.json()
    assert resumed_data["status"] == "Completed"
    assert resumed_data["human_approval_granted"] is True


def test_get_workflow_by_id():
    test_issue_id = str(uuid.uuid4())
    start_res = client.post(
        "/internal/workflows/start",
        headers=AUTH_HEADERS,
        json={
            "issue_id": test_issue_id,
            "issue_title": "Display console flickering",
            "description": "Display screen flickers at high speeds",
            "equipment_name": "Treadmill"
        }
    )
    wf_id = start_res.json()["id"]

    # Fetch it
    get_res = client.get(f"/internal/workflows/{wf_id}", headers=AUTH_HEADERS)
    assert get_res.status_code == 200
    assert get_res.json()["id"] == wf_id


def test_get_workflow_not_found():
    random_uuid = str(uuid.uuid4())
    get_res = client.get(f"/internal/workflows/{random_uuid}", headers=AUTH_HEADERS)
    assert get_res.status_code == 404


def test_cancel_workflow():
    test_issue_id = str(uuid.uuid4())
    start_res = client.post(
        "/internal/workflows/start",
        headers=AUTH_HEADERS,
        json={
            "issue_id": test_issue_id,
            "issue_title": "Cable tension adjustment",
            "description": "Cable feels slightly loose",
            "equipment_name": "Cable Crossover"
        }
    )
    wf_id = start_res.json()["id"]

    cancel_res = client.post(
        f"/internal/workflows/{wf_id}/cancel",
        headers=AUTH_HEADERS,
        json={"reason": "Reported by mistake, equipment working fine"}
    )
    assert cancel_res.status_code == 200
    assert cancel_res.json()["status"] == "Cancelled"
