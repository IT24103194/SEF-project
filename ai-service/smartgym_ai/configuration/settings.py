from pydantic_settings import BaseSettings, SettingsConfigDict
from typing import Optional

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    AI_SERVICE_API_KEY: str = "local-dev-internal-secret-token"
    AI_APPROVAL_COST_THRESHOLD: float = 25000.0
    LLM_PROVIDER: str = "mock"
    LLM_MODEL: str = "gpt-4o"
    LLM_API_KEY: Optional[str] = None
    LLM_BASE_URL: Optional[str] = None
    PORT: int = 8000
    HOST: str = "0.0.0.0"
    ENVIRONMENT: str = "development"

settings = Settings()
