import os
import sys
import pytest
from pydantic import BaseModel

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.validation.schema_validator import (
    SchemaValidator,
    MalformedJSONError,
    SchemaValidationError
)
from smartgym_ai.services.llm.mock_client import MockLLMClient


class DiagnosticOutput(BaseModel):
    issue_found: bool
    component: str
    repair_estimate_lkr: float


def test_malformed_json_raises_malformed_json_error():
    malformed_raw = "This is not JSON at all: { broken json: [1, 2"
    with pytest.raises(MalformedJSONError) as exc_info:
        SchemaValidator.parse_and_validate(malformed_raw, DiagnosticOutput)
    assert exc_info.value.raw_output == malformed_raw


def test_create_revision_prompt():
    raw = '{"component": "Belt"}'  # missing required fields
    try:
        SchemaValidator.parse_and_validate(raw, DiagnosticOutput)
    except SchemaValidationError as e:
        revision_prompt = SchemaValidator.generate_revision_prompt(
            original_output=raw,
            error=e,
            schema=DiagnosticOutput
        )
        assert "Please correct it according to the instructions below" in revision_prompt
        assert "issue_found" in revision_prompt
        assert "repair_estimate_lkr" in revision_prompt


@pytest.mark.asyncio
async def test_llm_malformed_output_retry_with_revision():
    """
    Simulate LLM outputting malformed text on first attempt,
    then after receiving revision prompt, outputting valid structured JSON.
    """
    first_malformed_response = "I think the motor is broken. Cost is roughly 15000."
    second_valid_response = '{"issue_found": true, "component": "Motor", "repair_estimate_lkr": 15000.0}'

    client = MockLLMClient()
    client.enqueue_response(first_malformed_response)
    client.enqueue_response(second_valid_response)

    # First attempt fails schema validation
    raw_1 = await client.generate([{"role": "user", "content": "Diagnose issue"}])
    with pytest.raises(MalformedJSONError) as err:
        SchemaValidator.parse_and_validate(raw_1, DiagnosticOutput)

    # Agent constructs revision prompt and retries
    rev_prompt = SchemaValidator.generate_revision_prompt(raw_1, err.value, DiagnosticOutput)
    raw_2 = await client.generate([{"role": "user", "content": rev_prompt}])

    # Second attempt succeeds
    validated = SchemaValidator.parse_and_validate(raw_2, DiagnosticOutput)
    assert validated.issue_found is True
    assert validated.component == "Motor"
    assert validated.repair_estimate_lkr == 15000.0
