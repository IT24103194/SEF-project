import os
import sys
import uuid
import pytest
from typing import Optional, Type
from pydantic import BaseModel

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.tools.base_tool import ToolBase, ToolResult
from smartgym_ai.tools.registry import ToolRegistry
from smartgym_ai.agents.base_agent import AgentBase, AgentResult
from smartgym_ai.models.workflow_models import WorkflowState, WorkflowStatus
from smartgym_ai.services.llm.mock_client import MockLLMClient


class DiagnosticInput(BaseModel):
    equipment_code: str
    error_code: str


class EquipmentTelemetryTool(ToolBase):
    @property
    def name(self) -> str:
        return "equipment_telemetry"

    @property
    def description(self) -> str:
        return "Fetches sensory telemetry data for gym equipment"

    @property
    def args_schema(self) -> Optional[Type[BaseModel]]:
        return DiagnosticInput

    async def _run(self, **kwargs) -> dict:
        return {
            "equipment_code": kwargs.get("equipment_code"),
            "error_code": kwargs.get("error_code"),
            "rpm": 1200,
            "temperature_celsius": 68.5,
            "status": "Warning"
        }


@pytest.mark.asyncio
async def test_tool_base_execution_and_metrics():
    tool = EquipmentTelemetryTool()
    result: ToolResult = await tool.execute(equipment_code="TM-001", error_code="ERR-404")

    assert result.is_success is True
    assert result.tool_name == "equipment_telemetry"
    assert result.data["rpm"] == 1200
    assert result.data["temperature_celsius"] == 68.5
    assert result.execution_time_ms >= 0


@pytest.mark.asyncio
async def test_tool_registry():
    registry = ToolRegistry()
    tool = EquipmentTelemetryTool()
    registry.register(tool)

    assert registry.get("equipment_telemetry") is not None

    specs = registry.list_tools()
    assert len(specs) == 1
    assert specs[0]["name"] == "equipment_telemetry"

    # Execute via registry
    result = await registry.execute_tool(
        "equipment_telemetry",
        args={"equipment_code": "TM-002", "error_code": "ERR-HEAT"}
    )
    assert result.is_success is True
    assert result.data["equipment_code"] == "TM-002"


class SimpleTestAgent(AgentBase):
    async def _process(self, state: WorkflowState, context: dict) -> dict:
        return {
            "assessment": "Completed test evaluation",
            "summary": "Evaluation finished cleanly",
            "tokens_used": 85
        }


@pytest.mark.asyncio
async def test_agent_base_lifecycle():
    mock_llm = MockLLMClient()
    agent = SimpleTestAgent(
        name="AssessmentAgent",
        role="Evaluator",
        system_prompt="You evaluate gym maintenance issues.",
        llm_client=mock_llm
    )
    assert agent.name == "AssessmentAgent"

    state = WorkflowState(
        id=uuid.uuid4(),
        issue_id=uuid.uuid4(),
        status=WorkflowStatus.Running,
        current_step="init"
    )

    result = await agent.execute(state)
    assert result.is_success is True
    assert result.agent_name == "AssessmentAgent"
    assert result.output_data["assessment"] == "Completed test evaluation"
    assert result.tokens_used == 85
