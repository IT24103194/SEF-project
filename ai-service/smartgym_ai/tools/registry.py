import threading
from typing import Optional, Any
from smartgym_ai.tools.base_tool import ToolBase, ToolResult
from smartgym_ai.services.resilience import TimeoutHandler
from smartgym_ai.configuration.settings import settings


class ToolRegistry:
    """Central catalog and secure execution orchestrator for agent tools."""

    def __init__(self):
        self._lock = threading.Lock()
        self._tools: dict[str, ToolBase] = {}

    def register(self, tool: ToolBase) -> None:
        """Register a tool in the catalog."""
        with self._lock:
            self._tools[tool.name] = tool

    def get(self, name: str) -> Optional[ToolBase]:
        """Look up tool by unique name."""
        with self._lock:
            return self._tools.get(name)

    def list_tools(self) -> list[dict[str, Any]]:
        """List registered tools with schema specifications."""
        with self._lock:
            tool_specs = []
            for t in self._tools.values():
                spec = {
                    "name": t.name,
                    "description": t.description,
                    "parameters": t.args_schema.model_json_schema() if t.args_schema else None
                }
                tool_specs.append(spec)
            return tool_specs

    async def execute_tool(
        self,
        name: str,
        args: dict[str, Any],
        timeout_seconds: Optional[float] = None
    ) -> ToolResult:
        """
        Execute tool safely within timeout and error bounds.
        """
        tool = self.get(name)
        if not tool:
            return ToolResult(
                tool_name=name,
                is_success=False,
                error=f"Tool '{name}' is not registered in the system."
            )

        timeout = timeout_seconds or settings.TOOL_TIMEOUT_SECONDS

        try:
            return await TimeoutHandler.execute_with_timeout(
                tool.execute(**args),
                timeout_seconds=timeout,
                operation_name=f"Tool[{name}]"
            )
        except Exception as e:
            return ToolResult(
                tool_name=name,
                is_success=False,
                error=f"Tool '{name}' failed: {str(e)}"
            )

    def clear(self) -> None:
        """Reset tool registry (used in tests)."""
        with self._lock:
            self._tools.clear()


# Default singleton registry
tool_registry = ToolRegistry()
