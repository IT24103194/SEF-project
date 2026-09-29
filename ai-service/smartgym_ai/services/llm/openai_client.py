import httpx
import json
from typing import Type, TypeVar, Optional, Any
from pydantic import BaseModel
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.validation.schema_validator import SchemaValidator
from smartgym_ai.services.resilience import RetryManager, TimeoutHandler, ServiceTimeoutError
from smartgym_ai.services.logging import ai_logger, get_correlation_id

T = TypeVar("T", bound=BaseModel)


class OpenAILLMClient(ILLMClient):
    """
    OpenAI-compatible LLM client adapter for cloud/local LLM providers
    (OpenAI, Azure, vLLM, Ollama, etc.).
    """

    def __init__(
        self,
        api_key: Optional[str] = None,
        model_name: str = "gpt-4o",
        base_url: Optional[str] = None,
        timeout_seconds: float = 30.0,
        max_retries: int = 3
    ):
        self._api_key = api_key or "sk-dummy-key"
        self._model_name = model_name
        self._base_url = (base_url or "https://api.openai.com/v1").rstrip("/")
        self.timeout_seconds = timeout_seconds
        self.max_retries = max_retries

    @property
    def provider_name(self) -> str:
        return "openai"

    @property
    def model_name(self) -> str:
        return self._model_name

    async def generate(
        self,
        messages: list[dict[str, str]],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        response_format: Optional[dict[str, Any]] = None,
        **kwargs: Any
    ) -> str:
        headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json"
        }

        payload: dict[str, Any] = {
            "model": self._model_name,
            "messages": messages,
            "temperature": temperature,
            "max_tokens": max_tokens,
        }
        if response_format:
            payload["response_format"] = response_format

        async with httpx.AsyncClient(timeout=self.timeout_seconds) as client:
            response = await client.post(
                f"{self._base_url}/chat/completions",
                json=payload,
                headers=headers
            )
            response.raise_for_status()
            data = response.json()
            return data["choices"][0]["message"]["content"]

    async def generate_structured(
        self,
        messages: list[dict[str, str]],
        schema: Type[T],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        **kwargs: Any
    ) -> T:
        schema_def = schema.model_json_schema()
        system_instruction = (
            f"You are a structured output generator. "
            f"Respond with a single valid JSON object strictly matching this schema:\n{json.dumps(schema_def)}"
        )

        conversation = [{"role": "system", "content": system_instruction}] + list(messages)
        retry_manager = RetryManager(max_retries=self.max_retries, backoff_factor=0.5)

        async def _attempt():
            raw = await self.generate(
                conversation,
                temperature=temperature,
                max_tokens=max_tokens,
                response_format={"type": "json_object"},
                **kwargs
            )
            try:
                return SchemaValidator.parse_and_validate(raw, schema)
            except Exception as err:
                ai_logger.warning(
                    f"OpenAI structured parsing failed: {err}. Appending revision prompt.",
                    extra={"correlation_id": get_correlation_id()}
                )
                revision_prompt = SchemaValidator.generate_revision_prompt(raw, err, schema)
                conversation.append({"role": "user", "content": revision_prompt})
                raise err

        return await retry_manager.execute_with_retry(
            _attempt,
            operation_name=f"OpenAILLMClient.generate_structured[{schema.__name__}]"
        )
