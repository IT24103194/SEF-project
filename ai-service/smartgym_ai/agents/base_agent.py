import time
from abc import ABC, abstractmethod
from typing import Optional, Any
from pydantic import BaseModel, Field
from smartgym_ai.models.workflow_models import WorkflowState
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.tools.base_tool import ToolBase
from smartgym_ai.services.logging import ai_logger, get_correlation_id


class AgentResult(BaseModel):
    """Encapsulates the output of a single agent step execution."""
    agent_name: str
    is_success: bool = True
    output_data: Optional[dict[str, Any]] = None
    next_step: Optional[str] = None
    summary: str = ""
    error: Optional[str] = None
    tokens_used: int = 0
    duration_ms: int = 0


class AgentBase(ABC):
    """
    Abstract Base Class for all SmartGym multi-agent role implementations.
    Defines role identity, system instructions, tooling access, and invocation lifecycle.
    """

    def __init__(
        self,
        name: str,
        role: str,
        system_prompt: str,
        llm_client: ILLMClient,
        tools: Optional[list[ToolBase]] = None
    ):
        self.name = name
        self.role = role
        self.system_prompt = system_prompt
        self.llm_client = llm_client
        self.tools = tools or []

    @abstractmethod
    async def _process(self, state: WorkflowState, context: dict[str, Any]) -> dict[str, Any]:
        """
        Core reasoning and execution step implemented by concrete agents.
        Must return a structured dictionary conforming to the step's contract.
        """
        pass

    async def execute(self, state: WorkflowState, context: Optional[dict[str, Any]] = None) -> AgentResult:
        """
        Executes the agent lifecycle, capturing telemetry, duration, and error boundary.
        """
        start = time.perf_counter()
        ctx = context or {}

        ai_logger.info(
            f"Agent '{self.name}' executing for workflow {state.id} (step: {state.current_step})",
            extra={
                "correlation_id": state.correlation_id or get_correlation_id(),
                "workflow_id": str(state.id),
                "extra_data": {"agent": self.name, "role": self.role}
            }
        )

        try:
            output = await self._process(state, ctx)
            elapsed_ms = int((time.perf_counter() - start) * 1000)

            return AgentResult(
                agent_name=self.name,
                is_success=True,
                output_data=output,
                next_step=output.get("next_step"),
                summary=output.get("summary", f"{self.name} completed successfully."),
                tokens_used=output.get("tokens_used", 100),
                duration_ms=elapsed_ms
            )
        except Exception as e:
            elapsed_ms = int((time.perf_counter() - start) * 1000)
            err_msg = str(e)

            ai_logger.error(
                f"Agent '{self.name}' failed on workflow {state.id}: {err_msg}",
                extra={
                    "correlation_id": state.correlation_id or get_correlation_id(),
                    "workflow_id": str(state.id),
                    "extra_data": {"agent": self.name, "error": err_msg}
                }
            )

            return AgentResult(
                agent_name=self.name,
                is_success=False,
                error=err_msg,
                summary=f"Agent '{self.name}' execution failed: {err_msg}",
                duration_ms=elapsed_ms
            )
