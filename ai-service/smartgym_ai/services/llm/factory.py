from typing import Optional
from smartgym_ai.configuration.settings import settings, Settings
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.services.llm.mock_client import MockLLMClient
from smartgym_ai.services.llm.openai_client import OpenAILLMClient
from smartgym_ai.services.logging import ai_logger


def get_llm_client(custom_settings: Optional[Settings] = None) -> ILLMClient:
    """
    Factory creating an ILLMClient instance according to the configured LLM_PROVIDER.
    Supports Google Gemini (via OpenAI-compatible endpoint), OpenAI, Azure, and local LLMs,
    with automatic graceful fallback to MockLLMClient if no valid API key is present.
    """
    cfg = custom_settings or settings
    provider = (cfg.LLM_PROVIDER or "mock").lower().strip()

    if provider in ("openai", "azure", "vllm", "ollama", "gemini"):
        key = cfg.LLM_API_KEY or cfg.GEMINI_API_KEY or cfg.OPENAI_API_KEY
        # Only instantiate cloud client if an actual API key is provided
        if key and key.strip() not in ("", "sk-dummy-key", "your_gemini_api_key_here", "your_openai_api_key_here"):
            base_url = cfg.LLM_BASE_URL
            model_name = cfg.LLM_MODEL
            if provider == "gemini":
                if not base_url:
                    base_url = "https://generativelanguage.googleapis.com/v1beta/openai"
                if not model_name or model_name.startswith("gpt"):
                    model_name = "gemini-1.5-flash"

            ai_logger.info(
                f"Initializing {provider.upper()} LLM client with model '{model_name}'",
                extra={"extra_data": {"provider": provider, "model": model_name, "base_url": base_url}}
            )

            return OpenAILLMClient(
                api_key=key,
                model_name=model_name or "gemini-1.5-flash",
                base_url=base_url,
                timeout_seconds=cfg.LLM_TIMEOUT_SECONDS,
                max_retries=cfg.MAX_RETRIES
            )
        else:
            ai_logger.info(
                f"No live API key found for provider '{provider}'. Falling back to deterministic MockLLMClient."
            )

    # Default fallback to mock client for testing, offline execution, and test suites
    return MockLLMClient(
        model_name=cfg.LLM_MODEL or "mock-model",
        max_retries=cfg.MAX_RETRIES,
        timeout_seconds=cfg.LLM_TIMEOUT_SECONDS
    )
