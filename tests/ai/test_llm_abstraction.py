import os
import sys
import pytest
from pydantic import BaseModel, Field

# Ensure ai-service is in sys.path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.services.llm.mock_client import MockLLMClient
from smartgym_ai.services.llm.openai_client import OpenAILLMClient
from smartgym_ai.services.llm.factory import get_llm_client
from smartgym_ai.configuration.settings import Settings


class SampleDiagnosticSchema(BaseModel):
    confidence_score: float = Field(ge=0.0, le=1.0)
    diagnosis: str
    recommended_action: str = ""
    estimated_cost: float = 0.0


@pytest.mark.asyncio
async def test_mock_llm_client_generate():
    client = MockLLMClient()
    client.enqueue_response("Diagnosis: Treadmill belt slipping.")
    assert isinstance(client, ILLMClient)

    response = await client.generate(
        messages=[
            {"role": "system", "content": "You are a gym equipment technician."},
            {"role": "user", "content": "Analyze error code E-01 on Treadmill"}
        ]
    )
    assert "Treadmill belt slipping" in response


@pytest.mark.asyncio
async def test_mock_llm_client_generate_structured():
    client = MockLLMClient()
    parsed = await client.generate_structured(
        messages=[
            {"role": "user", "content": "Analyze motor failure on Treadmill #4"}
        ],
        schema=SampleDiagnosticSchema
    )

    assert isinstance(parsed, SampleDiagnosticSchema)
    assert parsed.confidence_score > 0.0
    assert len(parsed.diagnosis) > 0


@pytest.mark.asyncio
async def test_mock_llm_client_queued_responses():
    client = MockLLMClient()
    client.enqueue_response("First response")
    client.enqueue_response("Second response")

    r1 = await client.generate([{"role": "user", "content": "test 1"}])
    r2 = await client.generate([{"role": "user", "content": "test 2"}])
    r3 = await client.generate([{"role": "user", "content": "test 3"}])  # falls back to default

    assert r1 == "First response"
    assert r2 == "Second response"
    assert len(r3) > 0


def test_openai_client_initialization():
    settings = Settings(
        LLM_PROVIDER="openai",
        LLM_API_KEY="test-openai-key-sk-dummy",
        LLM_MODEL="gpt-4o-mini",
        LLM_BASE_URL="https://api.openai.com/v1"
    )
    client = OpenAILLMClient(
        api_key=settings.LLM_API_KEY,
        model_name=settings.LLM_MODEL,
        base_url=settings.LLM_BASE_URL
    )
    assert isinstance(client, ILLMClient)
    assert client.model_name == "gpt-4o-mini"


def test_factory_returns_mock_client():
    settings = Settings(LLM_PROVIDER="mock")
    client = get_llm_client(settings)
    assert isinstance(client, MockLLMClient)


def test_factory_returns_openai_client():
    settings = Settings(
        LLM_PROVIDER="openai",
        LLM_API_KEY="test-key",
        LLM_MODEL="gpt-4o"
    )
    client = get_llm_client(settings)
    assert isinstance(client, OpenAILLMClient)
