import asyncio
import random
from typing import Callable, TypeVar, Any, Optional, Union
from datetime import datetime, timezone
from smartgym_ai.models.workflow_models import (
    WorkflowState,
    WorkflowStatus,
    WorkflowStepState,
    WorkflowStepStatus,
)
from smartgym_ai.services.logging import ai_logger, get_correlation_id

T = TypeVar("T")


class ServiceTimeoutError(Exception):
    """Raised when an operation exceeds its configured execution timeout limit."""
    pass


class MaxRetriesExceededError(Exception):
    """Raised when an operation has exhausted all configured retry attempts."""
    def __init__(self, message: str, last_exception: Optional[Exception] = None):
        super().__init__(message)
        self.last_exception = last_exception


class TimeoutHandler:
    """Bounded timeout handler for async operations."""

    @staticmethod
    async def execute_with_timeout(
        coro: Any,
        timeout_seconds: float,
        operation_name: str = "Operation"
    ) -> Any:
        try:
            return await asyncio.wait_for(coro, timeout=timeout_seconds)
        except asyncio.TimeoutError as e:
            ai_logger.warning(
                f"{operation_name} exceeded timeout bound of {timeout_seconds:.1f}s",
                extra={"correlation_id": get_correlation_id(), "extra_data": {"timeout_seconds": timeout_seconds, "operation": operation_name}}
            )
            raise ServiceTimeoutError(f"{operation_name} timed out after {timeout_seconds}s") from e


class RetryManager:
    """Bounded retry manager with exponential backoff and jitter."""

    def __init__(
        self,
        max_retries: int = 3,
        backoff_factor: float = 0.5,
        retryable_exceptions: tuple[type[Exception], ...] = (Exception,)
    ):
        self.max_retries = max_retries
        self.backoff_factor = backoff_factor
        self.retryable_exceptions = retryable_exceptions

    async def execute_with_retry(
        self,
        func: Callable[[], Any],
        operation_name: str = "Operation",
        on_retry: Optional[Callable[[int, Exception], Any]] = None
    ) -> Any:
        attempt = 0
        last_exception: Optional[Exception] = None

        while attempt <= self.max_retries:
            try:
                if asyncio.iscoroutinefunction(func):
                    return await func()
                result = func()
                if asyncio.iscoroutine(result):
                    return await result
                return result
            except self.retryable_exceptions as e:
                attempt += 1
                last_exception = e
                if attempt > self.max_retries:
                    break

                delay = (self.backoff_factor * (2 ** (attempt - 1))) + random.uniform(0.01, 0.1)
                ai_logger.warning(
                    f"{operation_name} failed attempt {attempt}/{self.max_retries}: {str(e)}. Retrying in {delay:.2f}s...",
                    extra={"correlation_id": get_correlation_id(), "extra_data": {"attempt": attempt, "error": str(e), "operation": operation_name}}
                )

                if on_retry:
                    if asyncio.iscoroutinefunction(on_retry):
                        await on_retry(attempt, e)
                    else:
                        on_retry(attempt, e)

                await asyncio.sleep(delay)

        ai_logger.error(
            f"{operation_name} exhausted all {self.max_retries} retries.",
            extra={"correlation_id": get_correlation_id(), "extra_data": {"last_error": str(last_exception), "operation": operation_name}}
        )
        raise MaxRetriesExceededError(
            f"{operation_name} failed after {self.max_retries} attempts: {str(last_exception)}",
            last_exception=last_exception
        )


class SafeFailureHandler:
    """
    Prevents cascading AI failures by transitioning the workflow state into a safe,
    audited failure representation with diagnostic details rather than crashing the runtime.
    """

    @staticmethod
    def handle_safe_failure(
        state: WorkflowState,
        error: Union[Exception, str],
        step_name: str = "Execution",
        fallback_action: Optional[str] = None
    ) -> WorkflowState:
        err_msg = str(error) if isinstance(error, Exception) else error
        err_type = type(error).__name__ if isinstance(error, Exception) else "Failure"

        ai_logger.error(
            f"Safe failure triggered for workflow {state.id} at step '{step_name}': {err_type} - {err_msg}",
            extra={
                "correlation_id": state.correlation_id,
                "workflow_id": str(state.id),
                "extra_data": {"step": step_name, "error_type": err_type, "error_message": err_msg}
            }
        )

        state.status = WorkflowStatus.Failed
        state.error_details = f"[{err_type}] {err_msg}"
        state.completed_at = datetime.now(timezone.utc)
        state.current_step = f"SafeFailure_{step_name}"

        if fallback_action:
            state.recommended_action = fallback_action
        elif not state.recommended_action:
            state.recommended_action = "Manual inspection required by maintenance team due to AI service safe failure."

        # Mark current or new step as failed
        existing_step = next((s for s in state.steps if s.step_name == step_name), None)
        if existing_step:
            existing_step.status = WorkflowStepStatus.Failed
            existing_step.summary = f"Step failed: {err_msg}"
        else:
            state.steps.append(
                WorkflowStepState(
                    step_name=step_name,
                    step_order=len(state.steps) + 1,
                    status=WorkflowStepStatus.Failed,
                    summary=f"Safe failure occurred: {err_msg}"
                )
            )

        return state
