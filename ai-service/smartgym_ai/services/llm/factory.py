from typing import Optional
from smartgym_ai.configuration.settings import settings, Settings
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.services.llm.mock_client import MockLLMClient
from smartgym_ai.services.llm.openai_client import OpenAILLMClient


def get_llm_client(custom_settings: Optional[Settings] = None) -> ILLMClient:
    """
    Factory creating an ILLMClient instance according to the configured LLM_PROVIDER.
    """
    cfg = custom_settings or settings
    provider = (cfg.LLM_PROVIDER or "mock").lower().strip()

    if provider == "mock":
        return MockLLMClient(
            model_name=cfg.LLM_MODEL,
            max_retries=cfg.MAX_RETRIES,
            timeout_seconds=cfg.LLM_TIMEOUT_SECONDS
        )
    elif provider in ("openai", "azure", "vllm", "ollama"):
        return OpenAILLMClient(
            api_key=cfg.LLM_API_KEY,
            model_name=cfg.LLM_MODEL,
            base_url=cfg.LLM_BASE_URL,
            timeout_seconds=cfg.LLM_TIMEOUT_SECONDS,
            max_retries=cfg.MAX_RETRIES
        )
    else:
        # Default fallback to mock to prevent crashing
        return MockLLMClient(model_name=cfg.LLM_MODEL)
