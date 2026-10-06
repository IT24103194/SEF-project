from pydantic_settings import BaseSettings, SettingsConfigDict
from pydantic import model_validator
from typing import Optional

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    # Internal Service Security
    AI_SERVICE_API_KEY: str = "local-dev-internal-secret-token"
    INTERNAL_API_KEY_HEADER: str = "X-Internal-Api-Key"
    CORRELATION_ID_HEADER: str = "X-Correlation-ID"

    # Governance & HITL Gates
    AI_APPROVAL_COST_THRESHOLD: float = 25000.0  # LKR (Rs.)

    # Database Persistence (PostgreSQL)
    DATABASE_URL: Optional[str] = "postgresql://postgres:1234@localhost:5432/smartgym"

    # LLM Provider Configuration
    LLM_PROVIDER: str = "mock"  # mock, gemini, openai, etc.
    LLM_MODEL: str = "gemini-1.5-flash"
    LLM_API_KEY: Optional[str] = None
    LLM_BASE_URL: Optional[str] = None
    LLM_TIMEOUT_SECONDS: float = 30.0

    # Specific Provider Key Aliases
    GEMINI_API_KEY: Optional[str] = None
    OPENAI_API_KEY: Optional[str] = None

    # Resilience & Execution Bounds
    MAX_RETRIES: int = 3
    RETRY_BACKOFF_FACTOR: float = 0.5
    TOOL_TIMEOUT_SECONDS: float = 10.0

    # Server Runtime
    PORT: int = 8000
    HOST: str = "0.0.0.0"
    ENVIRONMENT: str = "development"

    @model_validator(mode="after")
    def resolve_llm_configuration(self) -> "Settings":
        # Resolve API Key from aliases if LLM_API_KEY is not explicitly set
        if not self.LLM_API_KEY:
            if self.GEMINI_API_KEY:
                self.LLM_API_KEY = self.GEMINI_API_KEY
                if self.LLM_PROVIDER in ("mock", "", None):
                    self.LLM_PROVIDER = "gemini"
            elif self.OPENAI_API_KEY:
                self.LLM_API_KEY = self.OPENAI_API_KEY
                if self.LLM_PROVIDER in ("mock", "", None):
                    self.LLM_PROVIDER = "openai"

        # Auto-configure Gemini OpenAI-compatible endpoint
        if (self.LLM_PROVIDER or "").lower().strip() == "gemini":
            if not self.LLM_BASE_URL:
                self.LLM_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/openai"
            if not self.LLM_MODEL or self.LLM_MODEL.startswith("gpt"):
                self.LLM_MODEL = "gemini-1.5-flash"

        return self

settings = Settings()
