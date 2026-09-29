from pydantic_settings import BaseSettings, SettingsConfigDict
from typing import Optional

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    # Internal Service Security
    AI_SERVICE_API_KEY: str = "local-dev-internal-secret-token"
    INTERNAL_API_KEY_HEADER: str = "X-Internal-Api-Key"
    CORRELATION_ID_HEADER: str = "X-Correlation-ID"

    # Governance & HITL Gates
    AI_APPROVAL_COST_THRESHOLD: float = 25000.0  # LKR (Rs.)

    # LLM Provider Configuration
    LLM_PROVIDER: str = "mock"  # mock, openai, gemini, etc.
    LLM_MODEL: str = "gpt-4o"
    LLM_API_KEY: Optional[str] = None
    LLM_BASE_URL: Optional[str] = None
    LLM_TIMEOUT_SECONDS: float = 30.0

    # Resilience & Execution Bounds
    MAX_RETRIES: int = 3
    RETRY_BACKOFF_FACTOR: float = 0.5
    TOOL_TIMEOUT_SECONDS: float = 10.0

    # Server Runtime
    PORT: int = 8000
    HOST: str = "0.0.0.0"
    ENVIRONMENT: str = "development"

settings = Settings()
