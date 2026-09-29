import os
import sys
import asyncio
import pytest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.services.resilience import (
    TimeoutHandler,
    RetryManager,
    SafeFailureHandler,
    ServiceTimeoutError,
    MaxRetriesExceededError
)


@pytest.mark.asyncio
async def test_timeout_handler_completes_when_fast():
    async def fast_op():
        await asyncio.sleep(0.01)
        return "operation succeeded"

    result = await TimeoutHandler.execute_with_timeout(
        coro=fast_op(),
        timeout_seconds=1.0,
        operation_name="fast_operation"
    )
    assert result == "operation succeeded"


@pytest.mark.asyncio
async def test_timeout_handler_raises_service_timeout_error():
    async def slow_op():
        await asyncio.sleep(0.5)
        return "too late"

    with pytest.raises(ServiceTimeoutError) as exc_info:
        await TimeoutHandler.execute_with_timeout(
            coro=slow_op(),
            timeout_seconds=0.05,
            operation_name="slow_diagnostic"
        )
    assert "timed out after 0.05s" in str(exc_info.value)


@pytest.mark.asyncio
async def test_retry_manager_recovers_after_transient_failure():
    attempts = 0

    async def flaky_operation():
        nonlocal attempts
        attempts += 1
        if attempts < 2:
            raise ConnectionError("Network glitch")
        return "recovered"

    manager = RetryManager(max_retries=3, backoff_factor=0.01)
    result = await manager.execute_with_retry(
        func=flaky_operation,
        operation_name="flaky_llm_call"
    )
    assert result == "recovered"
    assert attempts == 2


@pytest.mark.asyncio
async def test_retry_manager_exhausts_retries():
    attempts = 0

    async def failing_operation():
        nonlocal attempts
        attempts += 1
        raise ValueError("Persistent bad response")

    manager = RetryManager(max_retries=3, backoff_factor=0.01)
    with pytest.raises(MaxRetriesExceededError) as exc_info:
        await manager.execute_with_retry(
            func=failing_operation,
            operation_name="always_failing_op"
        )
    assert attempts == 4  # Initial attempt (0) + 3 retries = 4 executions
    assert "Persistent bad response" in str(exc_info.value)
