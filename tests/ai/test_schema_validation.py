import os
import sys
import pytest
from pydantic import BaseModel, Field

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.validation.schema_validator import (
    SchemaValidator,
    MalformedJSONError,
    SchemaValidationError
)


class MaintenanceAssessment(BaseModel):
    equipment_name: str
    urgency_level: str
    estimated_cost_lkr: float = Field(ge=0.0)
    requires_replacement: bool
    recommended_parts: list[str] = []


def test_validate_clean_json():
    json_text = """
    {
        "equipment_name": "Treadmill Pro-500",
        "urgency_level": "High",
        "estimated_cost_lkr": 35000.0,
        "requires_replacement": true,
        "recommended_parts": ["Drive Motor", "Tension Belt"]
    }
    """
    model = SchemaValidator.parse_and_validate(json_text, MaintenanceAssessment)
    assert model.equipment_name == "Treadmill Pro-500"
    assert model.urgency_level == "High"
    assert model.estimated_cost_lkr == 35000.0
    assert model.requires_replacement is True
    assert len(model.recommended_parts) == 2


def test_validate_markdown_fenced_json():
    json_text = """
    ```json
    {
        "equipment_name": "Rowing Machine X",
        "urgency_level": "Low",
        "estimated_cost_lkr": 5000.0,
        "requires_replacement": false,
        "recommended_parts": ["Lubricant"]
    }
    ```
    """
    model = SchemaValidator.parse_and_validate(json_text, MaintenanceAssessment)
    assert model.equipment_name == "Rowing Machine X"
    assert model.urgency_level == "Low"
    assert model.requires_replacement is False


def test_validate_dirty_surrounding_text_json():
    json_text = """
    Based on our equipment diagnostic analysis, here is the assessment:
    {
        "equipment_name": "Stationary Bike B-2",
        "urgency_level": "Medium",
        "estimated_cost_lkr": 12000.0,
        "requires_replacement": false,
        "recommended_parts": ["Pedal strap"]
    }
    Please let us know if additional authorization is required.
    """
    model = SchemaValidator.parse_and_validate(json_text, MaintenanceAssessment)
    assert model.equipment_name == "Stationary Bike B-2"
    assert model.estimated_cost_lkr == 12000.0


def test_schema_validation_error_on_missing_field():
    # Missing required field 'equipment_name'
    json_text = """
    {
        "urgency_level": "High",
        "estimated_cost_lkr": 10000.0,
        "requires_replacement": false
    }
    """
    with pytest.raises(SchemaValidationError) as exc_info:
        SchemaValidator.parse_and_validate(json_text, MaintenanceAssessment)
    assert "equipment_name" in str(exc_info.value)


def test_schema_validation_error_on_invalid_type():
    # estimated_cost_lkr has negative value violating ge=0.0
    json_text = """
    {
        "equipment_name": "Treadmill",
        "urgency_level": "High",
        "estimated_cost_lkr": -500.0,
        "requires_replacement": false
    }
    """
    with pytest.raises(SchemaValidationError) as exc_info:
        SchemaValidator.parse_and_validate(json_text, MaintenanceAssessment)
    assert "estimated_cost_lkr" in str(exc_info.value)
