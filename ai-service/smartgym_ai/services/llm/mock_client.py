import asyncio
import json
from typing import Type, TypeVar, Optional, Any
from pydantic import BaseModel
from smartgym_ai.services.llm.client_interface import ILLMClient
from smartgym_ai.validation.schema_validator import SchemaValidator, MalformedJSONError
from smartgym_ai.services.resilience import RetryManager, TimeoutHandler, ServiceTimeoutError
from smartgym_ai.services.logging import ai_logger, get_correlation_id

T = TypeVar("T", bound=BaseModel)


class MockLLMClient(ILLMClient):
    """
    Mock LLM Client adapter for automated testing, offline verification,
    and predictable execution workflows.
    """

    def __init__(
        self,
        model_name: str = "mock-gpt-4o",
        simulated_delay: float = 0.0,
        should_timeout: bool = False,
        should_fail: bool = False,
        failure_error: Optional[Exception] = None,
        max_retries: int = 3,
        timeout_seconds: float = 30.0
    ):
        self._model_name = model_name
        self.simulated_delay = simulated_delay
        self.should_timeout = should_timeout
        self.should_fail = should_fail
        self.failure_error = failure_error
        self.max_retries = max_retries
        self.timeout_seconds = timeout_seconds

        self.queued_responses: list[str] = []
        self.call_count: int = 0
        self.history: list[list[dict[str, str]]] = []
        self.total_tokens_used: int = 0

    @property
    def provider_name(self) -> str:
        return "mock"

    @property
    def model_name(self) -> str:
        return self._model_name

    def enqueue_response(self, response: str) -> None:
        """Add a response to the sequential FIFO queue."""
        self.queued_responses.append(response)

    def enqueue_json_response(self, data: dict[str, Any]) -> None:
        """Helper to enqueue a dict serialized as JSON."""
        self.queued_responses.append(json.dumps(data))

    async def _execute_raw_call(self, messages: list[dict[str, str]], schema: Optional[Type[Any]] = None) -> str:
        self.call_count += 1
        self.history.append(messages)
        self.total_tokens_used += 150

        if self.should_timeout:
            # Simulate a hang exceeding timeout
            await asyncio.sleep(self.timeout_seconds + 0.5)
            raise ServiceTimeoutError("Mock client simulated timeout.")

        if self.simulated_delay > 0:
            await asyncio.sleep(self.simulated_delay)

        if self.should_fail:
            raise self.failure_error or RuntimeError("Mock client simulated error.")

        if self.queued_responses:
            # Check if an element in queue matches the requested schema
            for i, resp in enumerate(self.queued_responses):
                try:
                    parsed = json.loads(resp)
                    if isinstance(parsed, dict):
                        if schema and getattr(schema, "__name__", "") == "PlannerOutput":
                            if "steps" in parsed or "objective" in parsed:
                                return self.queued_responses.pop(i)
                        elif schema and getattr(schema, "__name__", "") == "SafetyValidationOutput":
                            if "contentSafe" in parsed or "content_safe" in parsed or "sanitizedDescription" in parsed:
                                return self.queued_responses.pop(i)
                        elif schema and getattr(schema, "__name__", "") == "DomainAnalysisOutput":
                            if "possibleIssue" in parsed or "possible_issue" in parsed or "priorityRecommendation" in parsed:
                                return self.queued_responses.pop(i)
                            elif "diagnosis_summary" in parsed or "diagnosis" in parsed or "estimated_cost" in parsed or "estimatedCost" in parsed:
                                popped = json.loads(self.queued_responses.pop(i))
                                return json.dumps({
                                    "possibleIssue": popped.get("possibleIssue") or popped.get("possible_issue") or popped.get("diagnosis_summary") or popped.get("diagnosis") or "Issue",
                                    "priorityRecommendation": popped.get("priorityRecommendation") or popped.get("priority_recommendation") or popped.get("severity") or "High",
                                    "requiredPart": popped.get("requiredPart") or popped.get("required_part") or "OEM-PART",
                                    "partAvailable": popped.get("partAvailable") if "partAvailable" in popped else popped.get("part_available", False),
                                    "recommendedSupplier": popped.get("recommendedSupplier") or popped.get("recommended_supplier") or "LifeFitness",
                                    "estimatedCost": float(popped.get("estimatedCost") or popped.get("estimated_cost") or 15000.0),
                                    "recommendedAction": popped.get("recommendedAction") or popped.get("recommended_action") or "Action",
                                    "supportingDataReferences": popped.get("supportingDataReferences") or popped.get("supporting_data_references") or []
                                })
                        elif schema and getattr(schema, "__name__", "") == "ActionAgentOutput":
                            if "proposedAction" in parsed or "proposed_action" in parsed:
                                return self.queued_responses.pop(i)
                        elif schema and getattr(schema, "__name__", "") == "DiagnosisOutputSchema":
                            if "estimated_cost" in parsed and ("diagnosis_summary" in parsed or "diagnosis" in parsed):
                                return self.queued_responses.pop(i)
                except Exception:
                    pass
            # If no schema-specific match, pop front if no schema was required
            if not schema:
                return self.queued_responses.pop(0)

        # Schema-specific default mock responses
        if schema and getattr(schema, "__name__", "") == "PlannerOutput":
            return json.dumps({
                "objective": "Resolve gym facility equipment issue",
                "plan_id": "11111111-1111-1111-1111-111111111111",
                "steps": [
                    {
                        "step_order": 1,
                        "step_name": "Validate facility issue",
                        "assigned_agent": "SafetyValidationAgent",
                        "reason": "Verify equipment safety, physical risk factors, and member impact.",
                        "approval_required": False
                    },
                    {
                        "step_order": 2,
                        "step_name": "Analyze equipment history",
                        "assigned_agent": "DomainAnalysisAgent",
                        "reason": "Review historical telemetry, past repairs, and manufacturer spec sheets.",
                        "approval_required": False
                    },
                    {
                        "step_order": 3,
                        "step_name": "Check relevant inventory",
                        "assigned_agent": "InventoryAgent",
                        "reason": "Verify if replacement drive belt or bearings are in gym stock.",
                        "approval_required": False
                    },
                    {
                        "step_order": 4,
                        "step_name": "Identify suitable supplier",
                        "assigned_agent": "SupplierAgent",
                        "reason": "Source OEM parts with guaranteed dispatch window if out of stock.",
                        "approval_required": False
                    },
                    {
                        "step_order": 5,
                        "step_name": "Prepare repair proposal",
                        "assigned_agent": "ProposalAgent",
                        "reason": "Draft itemized parts and technician labor cost estimate.",
                        "approval_required": False
                    },
                    {
                        "step_order": 6,
                        "step_name": "Validate proposal",
                        "assigned_agent": "SafetyValidationAgent",
                        "reason": "Ensure quote adheres to facility maintenance budget rules.",
                        "approval_required": False
                    },
                    {
                        "step_order": 7,
                        "step_name": "Request authorization",
                        "assigned_agent": "ApprovalGateAgent",
                        "reason": "Escalate to manager if cost exceeds 25,000 LKR approval threshold.",
                        "approval_required": False
                    },
                    {
                        "step_order": 8,
                        "step_name": "Execute approved action",
                        "assigned_agent": "ActionExecutionAgent",
                        "reason": "Dispatch certified technician and install verified replacement components.",
                        "approval_required": False
                    },
                    {
                        "step_order": 9,
                        "step_name": "Update ticket",
                        "assigned_agent": "TicketUpdateAgent",
                        "reason": "Close facility issue with timestamped audit notes and notify staff.",
                        "approval_required": False
                    }
                ],
                "assigned_agent": "SafetyValidationAgent",
                "reason": "Standard 9-step facility repair lifecycle with safety check and authorization gate.",
                "approval_required": False
            })

        if schema and getattr(schema, "__name__", "") == "SafetyValidationOutput":
            return json.dumps({
                "contentSafe": True,
                "sanitizedDescription": "Equipment belt slips during acceleration.",
                "ticketValid": True,
                "businessRulesSatisfied": True,
                "approvalRequired": False,
                "issues": [],
                "validationSummary": "All safety, content, and business validation rules successfully satisfied."
            })

        if schema and getattr(schema, "__name__", "") == "DomainAnalysisOutput":
            return json.dumps({
                "possibleIssue": "Drive belt wear or pulley bearing friction",
                "priorityRecommendation": "Medium",
                "requiredPart": "B-102",
                "partAvailable": True,
                "recommendedSupplier": "LifeFitness Certified Logistics",
                "estimatedCost": 15000.0,
                "recommendedAction": "Technician inspection and drive belt replacement",
                "supportingDataReferences": [
                    "Equipment Model: T-900 Pro (SN: SN-TM-2024-001)",
                    "Previous repair order #RO-2024-001 showed pulley lubrication",
                    "Inventory check: SKU B-102 available in Bin A-12",
                    "Supplier: LifeFitness Certified Logistics"
                ]
            })

        if schema and getattr(schema, "__name__", "") == "ActionAgentOutput":
            return json.dumps({
                "proposedAction": "Executed authorized repair order and notified supplier.",
                "repairOrderId": "22222222-2222-2222-2222-222222222222",
                "supplierId": "33333333-3333-3333-3333-333333333333",
                "estimatedCost": 15000.0,
                "executionPlan": [
                    "Created Repair Order",
                    "Dispatched Vendor RFQ",
                    "Updated Issue Status to VENDOR_CONTACTED",
                    "Created Staff Notification"
                ]
            })

        # Default fallback response for diagnosis
        return json.dumps({
            "diagnosis_summary": "Belt misalignment causing friction noise on treadmill motor pulley.",
            "diagnosis": "Belt misalignment causing friction noise on treadmill motor pulley.",
            "severity": "Medium",
            "estimated_cost": 15000.0,
            "requires_parts": True,
            "recommended_action": "Tension and align drive belt; inspect motor brushes.",
            "confidence_score": 0.92
        })

    async def generate(
        self,
        messages: list[dict[str, str]],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        schema: Optional[Type[Any]] = None,
        **kwargs: Any
    ) -> str:
        return await TimeoutHandler.execute_with_timeout(
            self._execute_raw_call(messages, schema=schema),
            timeout_seconds=self.timeout_seconds,
            operation_name="MockLLMClient.generate"
        )

    async def generate_structured(
        self,
        messages: list[dict[str, str]],
        schema: Type[T],
        temperature: float = 0.2,
        max_tokens: int = 2048,
        **kwargs: Any
    ) -> T:
        retry_manager = RetryManager(max_retries=self.max_retries, backoff_factor=0.05)
        conversation = list(messages)

        async def _attempt_call():
            raw_text = await self.generate(conversation, temperature=temperature, max_tokens=max_tokens, schema=schema, **kwargs)
            try:
                return SchemaValidator.parse_and_validate(raw_text, schema)
            except Exception as validation_err:
                ai_logger.warning(
                    f"Mock structured parsing failed: {validation_err}. Appending revision prompt.",
                    extra={"correlation_id": get_correlation_id()}
                )
                revision_prompt = SchemaValidator.generate_revision_prompt(raw_text, validation_err, schema)
                conversation.append({"role": "user", "content": revision_prompt})
                raise validation_err

        return await retry_manager.execute_with_retry(
            _attempt_call,
            operation_name=f"MockLLMClient.generate_structured[{schema.__name__}]"
        )
