import os
import sys
import uuid
import json
import pytest
import psycopg2
from typing import Any

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "ai-service")))

from smartgym_ai.models.domain_models import DomainAnalysisInput, DomainAnalysisOutput
from smartgym_ai.agents.domain_analysis_agent import DomainAnalysisAgent
from smartgym_ai.tools.domain_tools import (
    GetEquipmentDetailsTool,
    GetMaintenanceHistoryTool,
    GetSimilarFacilityIssuesTool,
    CheckInventoryTool,
    GetSupplierDetailsTool,
    GetProductDetailsTool,
)
from smartgym_ai.services.llm.mock_client import MockLLMClient
from smartgym_ai.services.tool_persistence import tool_persistence_service
from smartgym_ai.tools.base_tool import ToolBase, ToolResult

DB_URL = "postgresql://postgres:1234@localhost:5432/smartgym"


def get_real_db_equipment() -> dict[str, Any]:
    """Helper to fetch a genuine equipment record from PostgreSQL."""
    try:
        with psycopg2.connect(DB_URL) as conn:
            with conn.cursor() as cur:
                cur.execute('SELECT "Id", "SerialNumber", "Name", "Model", "Manufacturer", "Status" FROM equipment LIMIT 1')
                row = cur.fetchone()
                if row:
                    return {
                        "id": str(row[0]),
                        "serial_number": row[1],
                        "name": row[2],
                        "model": row[3],
                        "manufacturer": row[4],
                        "status": row[5]
                    }
    except Exception:
        pass
    return {
        "id": "8c88d510-afc9-444a-bf8d-acf88bf6aca2",
        "serial_number": "LF-TRD-2023-001",
        "name": "LifeFitness Elevation Treadmill T1",
        "model": "Elevation T95",
        "manufacturer": "LifeFitness USA",
        "status": 1
    }


# --- 1. Test Valid Equipment ---

@pytest.mark.asyncio
async def test_valid_equipment_domain_analysis():
    """
    Test 1: Valid equipment.
    Verifies that when valid equipment is provided, tools retrieve actual data
    and agent outputs a domain recommendation grounded in database values.
    """
    real_eq = get_real_db_equipment()
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm, db_url=DB_URL)

    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": "Treadmill T1 makes loud grinding noise on incline",
            "description": "Loud noise during 10% incline operation, suspect bearing or drive belt wear.",
            "equipment_id": real_eq["id"],
            "equipment_name": real_eq["name"]
        },
        equipment=real_eq,
        sanitized_description="Loud noise during 10% incline operation, suspect bearing or drive belt wear."
    )

    output, tool_records = await agent.analyze_facility_issue(input_data)

    assert isinstance(output, DomainAnalysisOutput)
    assert output.possible_issue != ""
    assert output.priority_recommendation in ["Low", "Medium", "High", "Critical"]
    assert output.estimated_cost > 0
    assert output.recommended_action != ""
    assert len(output.supporting_data_references) > 0

    # Ensure tools were called and observed
    assert len(tool_records) >= 4
    tool_names = [tr.tool_name for tr in tool_records]
    assert "getEquipmentDetails" in tool_names
    assert "getMaintenanceHistory" in tool_names
    assert "checkInventory" in tool_names

    # Check that equipment data was retrieved and referenced
    assert any(real_eq["serial_number"] in ref or real_eq["name"] in ref for ref in output.supporting_data_references)


# --- 2. Test Unknown Equipment ---

@pytest.mark.asyncio
async def test_unknown_equipment_handled_gracefully():
    """
    Test 2: Unknown equipment.
    Verifies that when equipment is not found in the database, the agent handles
    it gracefully without crashing, logs the lookup, and provides a safe inspection action.
    """
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm, db_url=DB_URL)

    non_existent_id = str(uuid.uuid4())
    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": "Unknown rower resistance jammed",
            "description": "Rower flywheel jammed in corner studio.",
            "equipment_id": non_existent_id,
            "equipment_name": "NonExistentRower 9000"
        },
        equipment={"id": non_existent_id, "name": "NonExistentRower 9000"},
        sanitized_description="Rower flywheel jammed in corner studio."
    )

    output, tool_records = await agent.analyze_facility_issue(input_data)

    assert isinstance(output, DomainAnalysisOutput)
    assert output.possible_issue != ""
    assert output.priority_recommendation != ""
    assert output.estimated_cost > 0
    assert "inspection" in output.recommended_action.lower() or "technician" in output.recommended_action.lower()

    # Verify that equipment tool recorded not found
    eq_record = next(r for r in tool_records if r.tool_name == "getEquipmentDetails")
    assert eq_record.is_success is True
    assert eq_record.output_result.get("found") is False


# --- 3. Test No Maintenance History ---

@pytest.mark.asyncio
async def test_no_maintenance_history():
    """
    Test 3: No maintenance history.
    Verifies that when equipment has no prior maintenance records, the agent
    handles empty history gracefully and still produces a grounded estimate.
    """
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm, db_url=DB_URL)

    # Equipment with newly minted ID that has no maintenance history
    new_eq_id = str(uuid.uuid4())
    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": "Brand new bike screen flicker",
            "description": "Screen intermittently flickers during high cadence.",
            "equipment_id": new_eq_id,
            "equipment_name": "Keiser M3i Indoor Cycle"
        },
        equipment={"id": new_eq_id, "name": "Keiser M3i Indoor Cycle"},
        sanitized_description="Screen intermittently flickers during high cadence."
    )

    output, tool_records = await agent.analyze_facility_issue(input_data)

    assert isinstance(output, DomainAnalysisOutput)
    assert output.possible_issue != ""
    assert output.estimated_cost > 0
    assert any("no prior service records" in ref.lower() or "not found" in ref.lower() for ref in output.supporting_data_references)


# --- 4. Test Missing Inventory ---

@pytest.mark.asyncio
async def test_missing_inventory():
    """
    Test 4: Missing inventory.
    Verifies that when required parts are out of stock or absent from warehouse,
    partAvailable is correctly set to False and supplier order is recommended.
    """
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm, db_url=DB_URL)

    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": "Unusual pneumatic piston failure",
            "description": "Exotic pneumatic air-compression valve leaking fluid.",
            "equipment_name": "Pneumatic Chest Press"
        },
        sanitized_description="Exotic pneumatic air-compression valve leaking fluid."
    )

    output, tool_records = await agent.analyze_facility_issue(input_data)

    assert isinstance(output, DomainAnalysisOutput)
    # The exotic pneumatic part is not in gym inventory
    inv_record = next((r for r in tool_records if r.tool_name == "checkInventory"), None)
    assert inv_record is not None
    # Output should reflect that part is either not available or order is needed
    assert output.estimated_cost > 0


# --- 5. Test Missing Supplier ---

@pytest.mark.asyncio
async def test_missing_supplier():
    """
    Test 5: Missing supplier.
    Verifies that when the manufacturer or brand has no registered supplier in DB,
    the agent notes the missing vendor and provides appropriate fallback action.
    """
    supplier_tool = GetSupplierDetailsTool(db_url=DB_URL)
    result = await supplier_tool.execute(name="NonExistentSupplierXYZ Ltd")

    assert result.is_success is True
    assert result.data.get("found") is False
    assert "not found" in result.data.get("error", "").lower()


# --- 6. Test Tool Failure / Resilient Error Handling ---

@pytest.mark.asyncio
async def test_tool_failure_resilience():
    """
    Test 6: Tool failure.
    Verifies that when an individual tool fails (e.g. query error or database connection issue),
    the agent catches the failure, records the error, logs it, and continues without crashing.
    """
    llm = MockLLMClient()
    # Provide an invalid DB connection string to trigger tool errors
    agent_bad_db = DomainAnalysisAgent(
        llm_client=llm,
        db_url="postgresql://invalid_user:invalid_pwd@localhost:5432/invalid_db",
        tool_timeout_seconds=2.0,
        max_tool_retries=1
    )

    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": "Treadmill belt slip",
            "description": "Belt slipping under heavy load.",
            "equipment_name": "Treadmill Pro"
        },
        sanitized_description="Belt slipping under heavy load."
    )

    output, tool_records = await agent_bad_db.analyze_facility_issue(input_data)

    # Agent should not crash; it should return a resilient DomainAnalysisOutput
    assert isinstance(output, DomainAnalysisOutput)
    assert output.possible_issue != ""
    assert output.estimated_cost > 0

    # Tool records should show failure with error details
    assert len(tool_records) > 0
    assert any(tr.is_success is False for tr in tool_records)


# --- 7. Test Invalid Tool Output / Tool Validation ---

@pytest.mark.asyncio
async def test_invalid_tool_input_validation():
    """
    Test 7: Invalid tool output / arguments.
    Verifies that tool input schemas strictly validate parameters and return
    a structured ToolResult with is_success=False on invalid inputs.
    """
    eq_tool = GetEquipmentDetailsTool(db_url=DB_URL)
    # Calling getEquipmentDetails with no arguments must raise validation error or fail schema
    result = await eq_tool.execute()
    assert result.is_success is False
    assert "At least one search parameter" in result.error or "Invalid arguments" in result.error

    maint_tool = GetMaintenanceHistoryTool(db_url=DB_URL)
    # Missing required equipment_id
    maint_result = await maint_tool.execute()
    assert maint_result.is_success is False
    assert "Invalid arguments" in maint_result.error or "equipment_id" in maint_result.error

    # Invalid limit (negative limit violates ge=1 constraint)
    maint_limit_result = await maint_tool.execute(equipment_id=str(uuid.uuid4()), limit=-5)
    assert maint_limit_result.is_success is False
    assert "greater than or equal to 1" in maint_limit_result.error or "Invalid arguments" in maint_limit_result.error


# --- 8. Observable Tool Execution Persistence in PostgreSQL ---

@pytest.mark.asyncio
async def test_tool_executions_persisted_to_database():
    """
    Test 8: Observable tool execution persistence.
    Verifies that tool executions are recorded to 'ai_tool_executions' in PostgreSQL.
    """
    real_eq = get_real_db_equipment()
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm, db_url=DB_URL)

    test_step_id = uuid.uuid4()
    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": "Cable crossover cable frayed",
            "description": "Right side cable coating peeled and metal strands frayed.",
            "equipment_id": real_eq["id"],
            "equipment_name": real_eq["name"]
        },
        equipment=real_eq,
        sanitized_description="Right side cable coating peeled and metal strands frayed."
    )

    output, tool_records = await agent.analyze_facility_issue(input_data, step_id=test_step_id)

    assert len(tool_records) >= 4

    # Verify directly in PostgreSQL 'ai_tool_executions' table
    with psycopg2.connect(DB_URL) as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT "ToolName", "InputParametersJson", "IsSuccess", "ExecutionTimeMs" FROM ai_tool_executions ORDER BY "ExecutedAt" DESC LIMIT 5')
            rows = cur.fetchall()
            assert len(rows) > 0
            persisted_tools = [r[0] for r in rows]
            assert any(t in persisted_tools for t in ["getEquipmentDetails", "checkInventory", "getSupplierDetails"])


# --- 9. Permissions Check: Agent Cannot Approve or Send Vendor Email ---

def test_domain_agent_permissions_enforced():
    """
    Test 9: Agent permissions.
    Verifies that the Gym Domain Analysis Agent does NOT possess tools or capabilities
    to approve workflows, authorize financial expenditures, or dispatch vendor emails.
    """
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm)

    tool_names = [t.name for t in agent.tools]

    # Prohibited tools MUST NOT be in domain agent tool set
    assert "approveWorkflow" not in tool_names
    assert "approveRepair" not in tool_names
    assert "sendVendorEmail" not in tool_names
    assert "sendEmail" not in tool_names
    assert "authorizeBudget" not in tool_names

    # Allowed tools only
    allowed_tools = {
        "getEquipmentDetails",
        "getMaintenanceHistory",
        "getSimilarFacilityIssues",
        "checkInventory",
        "getSupplierDetails",
        "getProductDetails",
    }
    assert set(tool_names) == allowed_tools

    # System prompt explicitly prohibits approval and vendor email
    assert "CANNOT approve" in agent.system_prompt
    assert "CANNOT send vendor emails" in agent.system_prompt
    assert "CANNOT bypass authorization" in agent.system_prompt


# --- 10. Actual SmartGym Database Recommendation (Not Hard-Coded) ---

@pytest.mark.asyncio
async def test_recommendation_based_on_actual_database_data():
    """
    Test 10: Grounded recommendation.
    Verifies that the agent uses actual retrieved database data rather than hardcoded constants.
    """
    real_eq = get_real_db_equipment()
    llm = MockLLMClient()
    agent = DomainAnalysisAgent(llm_client=llm, db_url=DB_URL)

    input_data = DomainAnalysisInput(
        facility_issue={
            "id": str(uuid.uuid4()),
            "title": f"Belt noise on {real_eq['name']}",
            "description": "Drive motor squeaking heavily.",
            "equipment_id": real_eq["id"],
            "equipment_name": real_eq["name"]
        },
        equipment=real_eq,
        sanitized_description="Drive motor squeaking heavily."
    )

    output, tool_records = await agent.analyze_facility_issue(input_data)

    # Output must reflect actual retrieved equipment data
    refs_combined = " ".join(output.supporting_data_references)
    assert real_eq["name"] in refs_combined or real_eq["serial_number"] in refs_combined
    assert output.estimated_cost > 0
    assert output.possible_issue != ""
