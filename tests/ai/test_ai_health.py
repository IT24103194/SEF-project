import sys
import os

# Add ai-service to sys.path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from fastapi.testclient import TestClient
from smartgym_ai.main import app

client = TestClient(app)

def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    assert data["service"] == "smartgym-ai"
    assert data["version"] == "1.0.0"
    assert "hitl_approval_threshold" in data

def test_service_info():
    response = client.get("/api/info")
    assert response.status_code == 200
    data = response.json()
    assert data["application"] == "SmartGym Agentic AI Microservice"
    assert len(data["agents"]) == 4
    agent_names = [a["name"] for a in data["agents"]]
    assert "Planner Agent" in agent_names
    assert "Safety & Business Validation Agent" in agent_names
    assert "Gym Domain Analysis Agent" in agent_names
    assert "Action / Tool Agent" in agent_names

def test_root_endpoint():
    response = client.get("/")
    assert response.status_code == 200
    data = response.json()
    assert "documentation" in data
