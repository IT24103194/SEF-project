import time
import pytest
from unittest.mock import patch

from smartgym_ai.models.action_models import (
    ApprovalStatus,
    ActionAgentInput,
    ActionAgentOutput,
)
from smartgym_ai.tools.action_tools import (
    CreateRepairOrderTool,
    PrepareVendorEmailTool,
    SendVendorEmailTool,
    UpdateFacilityIssueStatusTool,
    CreateNotificationTool,
)
from smartgym_ai.agents.action_execution_agent import ActionExecutionAgent


@pytest.fixture
def sample_domain_dict():
    return {
        "possible_issue": "Drive belt tensioner worn out and misaligned",
        "priority_recommendation": "High",
        "required_part": "Commercial Treadmill Drive Belt #TB-4002",
        "part_available": False,
        "recommended_supplier": "GymTech Supplies Co.",
        "estimated_cost": 420.0,
        "recommended_action": "Order replacement drive belt and schedule technician calibration",
        "supporting_data_references": ["equipment_history:T12", "supplier_catalog:SP-992"],
    }


@pytest.fixture
def sample_workflow_context():
    return {
        "workflow_id": "wf-test-1501",
        "issue_id": "issue-treadmill-12",
        "equipment_id": "eq-treadmill-12",
        "equipment_name": "Commercial Treadmill T12",
    }


def test_action_agent_initialization():
    agent = ActionExecutionAgent()
    assert agent.name == "ActionExecutionAgent"
    assert len(agent.tools) == 5
    tool_names = [t.name for t in agent.tools]
    assert "createRepairOrder" in tool_names
    assert "prepareVendorEmail" in tool_names
    assert "sendVendorEmail" in tool_names
    assert "updateFacilityIssueStatus" in tool_names
    assert "createNotification" in tool_names


@pytest.mark.asyncio
async def test_send_vendor_email_tool_rejects_without_approval():
    tool = SendVendorEmailTool()
    
    # 1. PENDING approval status must be rejected in application code
    with pytest.raises(PermissionError) as exc_info:
        await tool._run(
            recipient="parts@gymtech.com",
            subject="Purchase Order #RO-1234",
            body="Please dispatch drive belt.",
            approval_status="PENDING",
            idempotency_key="test-key-pending-1",
        )
    assert "Human approval" in str(exc_info.value)

    # 2. REJECTED status must be rejected in application code
    with pytest.raises(PermissionError) as exc_info2:
        await tool._run(
            recipient="parts@gymtech.com",
            subject="Purchase Order #RO-1234",
            body="Please dispatch drive belt.",
            approval_status="REJECTED",
            idempotency_key="test-key-rejected-1",
        )
    assert "Human approval" in str(exc_info2.value)


@pytest.mark.asyncio
async def test_action_before_approval_pauses_execution(sample_domain_dict, sample_workflow_context):
    agent = ActionExecutionAgent()
    agent_input = ActionAgentInput(
        domainRecommendation=sample_domain_dict,
        workflowContext=sample_workflow_context,
        approvalContext={"approval_status": "PENDING", "approved": False},
    )

    output, records = await agent.execute_action_plan(agent_input)

    # Verify that high-impact sendVendorEmail was NOT executed
    executed_tools = [r.tool_name for r in records]
    assert "sendVendorEmail" not in executed_tools

    # Verify that execution plan paused awaiting human approval
    assert any("paused" in step.lower() for step in output.executionPlan)


@pytest.mark.asyncio
async def test_approved_action_succeeds(sample_domain_dict, sample_workflow_context):
    agent = ActionExecutionAgent()
    agent_input = ActionAgentInput(
        domainRecommendation=sample_domain_dict,
        workflowContext=sample_workflow_context,
        approvalContext={
            "approval_status": "APPROVED",
            "approver": "admin@smartgym.com",
            "role": "ADMIN",
            "approved": True,
            "comments": "Proceed with urgent order",
        },
    )

    output, records = await agent.execute_action_plan(agent_input)

    assert isinstance(output, ActionAgentOutput)
    assert output.estimatedCost == 420.0

    # High-impact tool sendVendorEmail and updateFacilityIssueStatus were executed
    executed_tools = [r.tool_name for r in records]
    assert "createRepairOrder" in executed_tools
    assert "prepareVendorEmail" in executed_tools
    assert "sendVendorEmail" in executed_tools
    assert "updateFacilityIssueStatus" in executed_tools
    assert "createNotification" in executed_tools


@pytest.mark.asyncio
async def test_changed_proposal_after_approval_requires_revalidation(sample_domain_dict, sample_workflow_context):
    agent = ActionExecutionAgent()

    # Proposal cost is now 9850.0 instead of original approved 420.0
    altered_domain_dict = dict(sample_domain_dict)
    altered_domain_dict["estimated_cost"] = 9850.0

    agent_input = ActionAgentInput(
        domainRecommendation=altered_domain_dict,
        workflowContext=sample_workflow_context,
        approvalContext={
            "approval_status": "APPROVED",
            "approved_estimated_cost": 420.0, # Previously approved cost
            "approver": "admin@smartgym.com",
            "role": "ADMIN",
        },
    )

    output, records = await agent.execute_action_plan(agent_input)

    # Because proposal changed after approval, high-impact dispatch must NOT execute
    executed_tools = [r.tool_name for r in records]
    assert "sendVendorEmail" not in executed_tools

    # Execution plan notes revalidation requirement
    assert any("revalidation required" in step.lower() for step in output.executionPlan)


@pytest.mark.asyncio
async def test_duplicate_vendor_action_prevented():
    tool = SendVendorEmailTool()
    key = f"idem-vendor-test-{time.time()}"

    # First dispatch with valid approval succeeds
    res1 = await tool._run(
        recipient="vendor@gymsupplies.com",
        subject="Order RO-1",
        body="Order body",
        approval_status="APPROVED",
        idempotency_key=key,
    )
    assert res1["sent"] is True

    # Second dispatch with identical idempotency key is detected and prevented
    res2 = await tool._run(
        recipient="vendor@gymsupplies.com",
        subject="Order RO-1",
        body="Order body",
        approval_status="APPROVED",
        idempotency_key=key,
    )
    assert res2["sent"] is False
    assert res2["is_duplicate"] is True
    assert "duplicate" in res2["message"].lower()


@pytest.mark.asyncio
async def test_action_agent_run_method(sample_domain_dict, sample_workflow_context):
    agent = ActionExecutionAgent()
    agent_input = ActionAgentInput(
        domainRecommendation=sample_domain_dict,
        workflowContext=sample_workflow_context,
        approvalContext={
            "approval_status": "APPROVED",
            "approver": "admin@smartgym.com",
            "role": "ADMIN",
        },
    )

    result = await agent.run(agent_input)
    assert isinstance(result, ActionAgentOutput)
    assert result.estimatedCost == 420.0
    assert len(result.executionPlan) > 0
