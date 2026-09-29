import re
import json
from typing import Type, TypeVar, Optional, Any
from pydantic import BaseModel, ValidationError

T = TypeVar("T", bound=BaseModel)


class MalformedJSONError(Exception):
    """Raised when the LLM response is not valid JSON."""
    def __init__(self, raw_output: str, parse_error: str):
        super().__init__(f"Malformed JSON in LLM response: {parse_error}")
        self.raw_output = raw_output
        self.parse_error = parse_error


class SchemaValidationError(Exception):
    """Raised when the LLM response does not conform to the expected Pydantic schema."""
    def __init__(self, raw_json: dict[str, Any], validation_error: ValidationError):
        super().__init__(f"Schema validation failed: {str(validation_error)}")
        self.raw_json = raw_json
        self.validation_error = validation_error


class SchemaValidator:
    """Utilities for extracting, parsing, and validating structured LLM JSON outputs."""

    @staticmethod
    def extract_json_str(raw_text: str) -> str:
        """
        Extracts JSON content from raw LLM output, stripping markdown code blocks
        (e.g., ```json ... ```) and leading/trailing non-JSON text.
        """
        if not raw_text or not raw_text.strip():
            raise MalformedJSONError(raw_text, "Empty output from model.")

        text = raw_text.strip()

        # Check for markdown code blocks (```json ... ``` or ``` ... ```)
        md_match = re.search(r"```(?:json)?\s*([\s\S]*?)\s*```", text, re.IGNORECASE)
        if md_match:
            text = md_match.group(1).strip()

        # Extract outermost curly braces or brackets if surrounded by commentary
        brace_start = text.find("{")
        bracket_start = text.find("[")

        if brace_start != -1 and (bracket_start == -1 or brace_start < bracket_start):
            brace_end = text.rfind("}")
            if brace_end != -1 and brace_end > brace_start:
                text = text[brace_start:brace_end + 1]
        elif bracket_start != -1:
            bracket_end = text.rfind("]")
            if bracket_end != -1 and bracket_end > bracket_start:
                text = text[bracket_start:bracket_end + 1]

        return text

    @classmethod
    def parse_and_validate(cls, raw_output: str, schema: Type[T]) -> T:
        """
        Extracts, parses, and validates the model output against the target Pydantic schema.
        Raises MalformedJSONError or SchemaValidationError on failure.
        """
        json_str = cls.extract_json_str(raw_output)

        try:
            parsed_dict = json.loads(json_str)
        except json.JSONDecodeError as e:
            raise MalformedJSONError(raw_output, str(e)) from e

        if not isinstance(parsed_dict, dict):
            raise MalformedJSONError(raw_output, f"Expected JSON object, got {type(parsed_dict).__name__}")

        try:
            return schema.model_validate(parsed_dict)
        except ValidationError as e:
            raise SchemaValidationError(parsed_dict, e) from e

    @classmethod
    def generate_revision_prompt(cls, original_output: str, error: Exception, schema: Type[T]) -> str:
        """
        Constructs a structured correction prompt to send back to the LLM during retry/revision cycles.
        """
        schema_json = json.dumps(schema.model_json_schema(), indent=2)
        err_msg = str(error)

        return (
            "Your previous response could not be validated. Please correct it according to the instructions below.\n\n"
            f"ERROR DETAILS:\n{err_msg}\n\n"
            f"REQUIRED JSON SCHEMA:\n{schema_json}\n\n"
            f"YOUR PREVIOUS RESPONSE:\n{original_output}\n\n"
            "CRITICAL INSTRUCTION: Return ONLY a valid JSON object matching the required schema. "
            "Do NOT include any surrounding explanation, greeting, or Markdown formatting outside of the JSON."
        )
