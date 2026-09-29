import time
from abc import ABC, abstractmethod
from typing import Type, Optional, Any
from pydantic import BaseModel, Field


class ToolResult(BaseModel):
    """Encapsulates the standard output contract for any executed tool."""
    tool_name: str
    is_success: bool = True
    data: Optional[Any] = None
    error: Optional[str] = None
    execution_time_ms: int = 0


class ToolBase(ABC):
    """
    Abstract Base Class for all tools available to SmartGym AI agents.
    Provides schema definition, argument validation, and timed execution.
    """

    @property
    @abstractmethod
    def name(self) -> str:
        """Unique identifier of the tool."""
        pass

    @property
    @abstractmethod
    def description(self) -> str:
        """Clear purpose description for the planner and LLM agents."""
        pass

    @property
    def args_schema(self) -> Optional[Type[BaseModel]]:
        """Optional Pydantic schema defining expected input parameters."""
        return None

    @abstractmethod
    async def _run(self, **kwargs: Any) -> Any:
        """Internal execution implementation."""
        pass

    async def execute(self, **kwargs: Any) -> ToolResult:
        """
        Executes the tool with parameter validation and execution time tracking.
        """
        start = time.perf_counter()

        # Validate arguments if schema is defined
        if self.args_schema is not None:
            try:
                validated_args = self.args_schema.model_validate(kwargs)
                kwargs = validated_args.model_dump()
            except Exception as e:
                elapsed_ms = int((time.perf_counter() - start) * 1000)
                return ToolResult(
                    tool_name=self.name,
                    is_success=False,
                    error=f"Invalid arguments for tool '{self.name}': {str(e)}",
                    execution_time_ms=elapsed_ms
                )

        try:
            output = await self._run(**kwargs)
            elapsed_ms = int((time.perf_counter() - start) * 1000)
            return ToolResult(
                tool_name=self.name,
                is_success=True,
                data=output,
                execution_time_ms=elapsed_ms
            )
        except Exception as e:
            elapsed_ms = int((time.perf_counter() - start) * 1000)
            return ToolResult(
                tool_name=self.name,
                is_success=False,
                error=str(e),
                execution_time_ms=elapsed_ms
            )
