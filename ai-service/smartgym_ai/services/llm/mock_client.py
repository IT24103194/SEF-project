import asyncio
import json
from typing import Type, TypeVar, Optional, Any
from pydantic import BaseModel
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.validation.schema_validator import SchemaValidator, MalformedJSONError
from smartgym_ai.services.resilience import RetryManager, TimeoutHandler, ServiceTimeoutError
from smartgym_ai.services.logging import ai_logger, get_correlation_id

T = TypeVar("T", bound=BaseModel)


class MockLLMClient(ILLMClient):
    """
    Mock LLM Client adapter for automated testing, offline verification,
    and predictable execution workflows.
    """

    def __init__(
        self,
        model_name: str = "mock-gpt-4o",
        simulated_delay: float = 0.0,
        should_timeout: bool = False,
        should_fail: bool = False,
        failure_error: Optional[Exception] = None,
        max_retries: int = 3,
        timeout_seconds: float = 30.0
    ):
        self._model_name = model_name
        self.simulated_delay = simulated_delay
        self.should_timeout = should_timeout
        self.should_fail = should_fail
        self.failure_error = failure_error
        self.max_retries = max_retries
        self.timeout_seconds = timeout_seconds

        self.queued_responses: list[str] = []
        self.call_count: int = 0
        self.history: list[list[dict[str, str]]] = []
        self.total_tokens_used: int = 0

    @property
    def provider_name(self) -> str:
        return "mock"

    @property
    def model_name(self) -> str:
        return self._model_name

    def enqueue_response(self, response: str) -> None:
        """Add a response to the sequential FIFO queue."""
        self.queued_responses.append(response)

    def enqueue_json_response(self, data: dict[str, Any]) -> None:
        """Helper to enqueue a dict serialized as JSON."""
        self.queued_responses.append(json.dumps(data))

    async def _execute_raw_call(self, messages: list[dict[str, str]]) -> str:
        self.call_count += 1
        self.history.append(messages)
        self.total_tokens_used += 150

        if self.should_timeout:
            # Simulate a hang exceeding timeout
            await asyncio.sleep(self.timeout_seconds + 0.5)
            raise ServiceTimeoutError("Mock client simulated timeout.")

        if self.simulated_delay > 0:
            await asyncio.sleep(self.simulated_delay)

        if self.should_fail:
            raise self.failure_error or RuntimeError("Mock client simulated error.")

        if self.queued_responses:
            return self.queued_responses.pop(0)

        # Default fallback response
        return json.dumps({
            "diagnosis_summary": "Belt misalignment causing friction noise on treadmill motor pulley.",
            "diagnosis": "Belt misalignment causing friction noise on treadmill motor pulley.",
            "severity": "Medium",
            "estimated_cost": 15000.0,
            "requires_parts": True,
            "recommended_action": "Tension and align drive belt; inspect motor brushes.",
            "confidence_score": 0.92
        })

    async def generate(
        self,
        messages: list[dict[str, str]],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        **kwargs: Any
    ) -> str:
        return await TimeoutHandler.execute_with_timeout(
            self._execute_raw_call(messages),
            timeout_seconds=self.timeout_seconds,
            operation_name="MockLLMClient.generate"
        )

    async def generate_structured(
        self,
        messages: list[dict[str, str]],
        schema: Type[T],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        **kwargs: Any
    ) -> T:
        retry_manager = RetryManager(max_retries=self.max_retries, backoff_factor=0.05)
        conversation = list(messages)

        async def _attempt_call():
            raw_text = await self.generate(conversation, temperature=temperature, max_tokens=max_tokens, **kwargs)
            try:
                return SchemaValidator.parse_and_validate(raw_text, schema)
            except Exception as validation_err:
                ai_logger.warning(
                    f"Mock structured parsing failed: {validation_err}. Appending revision prompt.",
                    extra={"correlation_id": get_correlation_id()}
                )
                revision_prompt = SchemaValidator.generate_revision_prompt(raw_text, validation_err, schema)
                conversation.append({"role": "user", "content": revision_prompt})
                raise validation_err

        return await retry_manager.execute_with_retry(
            _attempt_call,
            operation_name=f"MockLLMClient.generate_structured[{schema.__name__}]"
        )
