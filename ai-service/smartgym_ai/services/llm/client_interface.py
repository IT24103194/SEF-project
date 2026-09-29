from abc import ABC, abstractmethod
from typing import Type, TypeVar, Optional, Any
from pydantic import BaseModel

T = TypeVar("T", bound=BaseModel)


class ILLMClient(ABC):
    """
    Core LLM provider client abstraction.
    Enforces standardized interaction with language models regardless of provider.
    """

    @abstractmethod
    async def generate(
        self,
        messages: list[dict[str, str]],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        **kwargs: Any
    ) -> str:
        """
        Generate raw text/JSON from model given a message list.
        """
        pass

    @abstractmethod
    async def generate_structured(
        self,
        messages: list[dict[str, str]],
        schema: Type[T],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        **kwargs: Any
    ) -> T:
        """
        Generate and validate response matching a target Pydantic schema.
        Handles formatting, parsing, and automated retry/revision on schema errors.
        """
        pass

    @property
    @abstractmethod
    def provider_name(self) -> str:
        """Returns the provider name identifier (e.g. 'mock', 'openai', 'gemini')."""
        pass

    @property
    @abstractmethod
    def model_name(self) -> str:
        """Returns the configured model identifier."""
        pass
