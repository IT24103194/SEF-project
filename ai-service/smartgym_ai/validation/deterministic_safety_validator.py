import json
from typing import Optional, Any
from uuid import UUID, uuid4
from datetime import datetime, timezone

from smartgym_ai.models.safety_models import SafetyValidationOutput
from smartgym_ai.tools.safety_tools import (
    ModerateTextTool,
    ValidateTicketTool,
    ValidateEquipmentTool,
    ValidatePriorityTool,
    ValidateRepairAmountTool,
    ValidateRequiredFieldsTool,
)
from smartgym_ai.models.planner_models import SupportedAgent
from smartgym_ai.configuration.settings import settings
from smartgym_ai.services.logging import ai_logger


class DeterministicSafetyValidator:
    """
    Authoritative deterministic validation engine for SmartGym AI.
    Executes concrete application-code business rules, safety checks,
    content moderation, and permission boundaries independently of LLM reasoning.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL
        self.moderate_tool = ModerateTextTool()
        self.ticket_tool = ValidateTicketTool()
        self.equipment_tool = ValidateEquipmentTool()
        self.priority_tool = ValidatePriorityTool()
        self.amount_tool = ValidateRepairAmountTool()
        self.fields_tool = ValidateRequiredFieldsTool()

    async def validate(
        self,
        facility_issue: dict[str, Any],
        planner_output: Optional[dict[str, Any]] = None,
        workflow_context: Optional[dict[str, Any]] = None,
        persist_records: bool = True
    ) -> SafetyValidationOutput:
        ctx = workflow_context or {}
        issue = facility_issue or {}
        plan = planner_output or {}

        issues: list[str] = []
        content_safe = True
        ticket_valid = True
        business_rules_satisfied = True
        approval_required = False

        # --- 1. Content Moderation & Sanitization ---
        raw_description = str(issue.get("description") or "").strip()
        raw_title = str(issue.get("title") or issue.get("issue_title") or "").strip()

        mod_desc = await self.moderate_tool._run(text=raw_description)
        mod_title = await self.moderate_tool._run(text=raw_title)

        sanitized_description = mod_desc["sanitized_text"]

        if not mod_desc["is_safe"] or not mod_title["is_safe"]:
            content_safe = False
            reasons = []
            if mod_desc.get("moderation_reason"):
                reasons.append(mod_desc["moderation_reason"])
            if mod_title.get("moderation_reason"):
                reasons.append(mod_title["moderation_reason"])
            issues.append(f"Content moderation flagged: {'; '.join(reasons)}")

        if mod_desc.get("prompt_injection_detected") or mod_title.get("prompt_injection_detected"):
            content_safe = False
            business_rules_satisfied = False
            issues.append("Unsafe prompt injection / adversarial instruction detected in ticket content.")

        # --- 2. Required Data & Ticket Structure Validation ---
        ticket_id = issue.get("id") or issue.get("issue_id")
        t_res = await self.ticket_tool._run(
            ticket_id=ticket_id,
            title=raw_title,
            description=raw_description,
            severity=issue.get("severity") or issue.get("priority")
        )
        if not t_res["is_valid"]:
            ticket_valid = False
            business_rules_satisfied = False
            issues.extend(t_res["errors"])

        # --- 3. Equipment Must Exist Rule ---
        equipment_id = issue.get("equipment_id")
        equipment_name = issue.get("equipment_name") or ctx.get("equipment_name")
        eq_res = await self.equipment_tool._run(
            equipment_id=equipment_id,
            equipment_name=equipment_name,
            db_url=self.db_url
        )
        if not eq_res["is_valid"]:
            ticket_valid = False
            business_rules_satisfied = False
            issues.append(f"Equipment rule violation: {eq_res.get('reason')}")

        # --- 4. Priority / Severity Validation Rule ---
        priority = issue.get("severity") or issue.get("priority")
        if priority is not None:
            p_res = await self.priority_tool._run(priority=priority)
            if not p_res["is_valid"]:
                ticket_valid = False
                business_rules_satisfied = False
                issues.append(f"Priority rule violation: {p_res.get('reason')}")

        # --- 5. Cost & Repair Amount Rule ---
        cost_val = (
            ctx.get("estimated_cost")
            or issue.get("estimated_cost")
            or plan.get("estimated_cost")
        )
        if cost_val is not None:
            try:
                amt_res = await self.amount_tool._run(amount=float(cost_val))
                if not amt_res["is_valid"]:
                    business_rules_satisfied = False
                    issues.append(f"Financial rule violation: {amt_res.get('reason')}")
                if amt_res["approval_required"]:
                    approval_required = True
            except (ValueError, TypeError):
                business_rules_satisfied = False
                issues.append(f"Financial rule violation: Invalid cost format '{cost_val}'.")

        # Plan approval requirement propagation
        if plan.get("approval_required") or plan.get("requires_human_approval"):
            approval_required = True

        # --- 6. Member Cannot Approve Own Request Rule ---
        action = str(ctx.get("action") or "").strip().lower()
        actor_id = ctx.get("user_id") or ctx.get("approver_id") or ctx.get("actor_id")
        member_id = issue.get("reported_by_member_id") or issue.get("reported_by_user_id")

        if actor_id and member_id and str(actor_id).strip().lower() == str(member_id).strip().lower():
            if action in ("approve", "approved", "request_approval", "grant_approval"):
                business_rules_satisfied = False
                issues.append("Conflict of interest: Member cannot approve their own facility request.")

        # --- 7. Only Admin Can Approve Rule ---
        actor_role = str(ctx.get("user_role") or ctx.get("role") or ctx.get("actor_role") or "").strip().upper()
        if action in ("approve", "approved", "grant_approval"):
            if actor_role != "ADMIN":
                business_rules_satisfied = False
                issues.append(
                    f"Unauthorized approval request: Only Admin can approve repair workflows (user has role '{actor_role or 'Unknown'}')."
                )

        # --- 8. Unsafe Action is Rejected Rule ---
        # Inspect planned steps or requested action for dangerous or bypassing actions
        planned_steps = plan.get("steps") or []
        for step in planned_steps:
            step_name = str(step.get("step_name") or "").lower()
            if any(term in step_name for term in ["bypass safety", "override interlock", "touch live wire", "disable breaker"]):
                business_rules_satisfied = False
                issues.append(f"Unsafe action rejected: Planned step '{step.get('step_name')}' violates physical safety guidelines.")

        # --- 9. Unsupported Operation & Permission Boundaries ---
        # The Planner / Safety Agent cannot send vendor emails, approve repairs, or execute payments directly
        for step in planned_steps:
            assigned = str(step.get("assigned_agent") or "").lower()
            step_name = str(step.get("step_name") or "").lower()
            if "safety" in assigned or "planner" in assigned:
                if any(disallowed in step_name for disallowed in ["send vendor email", "approve repair", "execute payment", "wire funds"]):
                    business_rules_satisfied = False
                    issues.append(
                        f"Unsupported operation rejected: Agent '{step.get('assigned_agent')}' cannot execute high-impact action '{step.get('step_name')}'."
                    )

        # --- Synthesis & Summary ---
        if issues:
            summary = f"Validation failed with {len(issues)} issue(s): " + "; ".join(issues)
        else:
            summary = "All safety, content, and business validation rules successfully satisfied."

        output = SafetyValidationOutput(
            contentSafe=content_safe,
            sanitizedDescription=sanitized_description,
            ticketValid=ticket_valid,
            businessRulesSatisfied=business_rules_satisfied,
            approvalRequired=approval_required,
            issues=issues,
            validationSummary=summary
        )

        # --- 10. Persist Moderation and Audit Records ---
        if persist_records and ticket_id:
            await self._persist_records(
                ticket_id=ticket_id,
                output=output,
                user_id=actor_id
            )

        return output

    async def _persist_records(
        self,
        ticket_id: Any,
        output: SafetyValidationOutput,
        user_id: Optional[Any] = None
    ) -> None:
        """
        Stores moderation results and audit records into PostgreSQL if available.
        Ensures raw unmasked offensive content is never unnecessarily exposed in logs.
        """
        if not self.db_url:
            return

        try:
            import psycopg2
            with psycopg2.connect(self.db_url) as conn:
                with conn.cursor() as cur:
                    # 1. Update facility_issues moderation columns
                    cur.execute(
                        """
                        UPDATE facility_issues
                        SET "SanitizedDescription" = %s,
                            "ModerationStatus" = %s,
                            "ModerationReason" = %s,
                            "UpdatedAt" = NOW()
                        WHERE "Id" = %s
                        """,
                        (
                            output.sanitized_description,
                            "Clean" if output.content_safe else "Flagged",
                            "; ".join(output.issues) if not output.content_safe else None,
                            str(ticket_id)
                        )
                    )

                    # 2. Insert audit log record
                    audit_id = str(uuid4())
                    now = datetime.now(timezone.utc)
                    audit_payload = {
                        "contentSafe": output.content_safe,
                        "ticketValid": output.ticket_valid,
                        "businessRulesSatisfied": output.business_rules_satisfied,
                        "approvalRequired": output.approval_required,
                        "issues": output.issues,
                        "sanitizedDescription": output.sanitized_description
                    }

                    cur.execute(
                        """
                        INSERT INTO audit_logs (
                            "Id", "EntityName", "EntityId", "Action", "UserId",
                            "Timestamp", "NewValuesJson"
                        ) VALUES (
                            %s, %s, %s, %s, %s, %s, %s
                        )
                        """,
                        (
                            audit_id,
                            "FacilityIssue",
                            str(ticket_id),
                            "SAFETY_VALIDATION",
                            str(user_id) if user_id else None,
                            now,
                            json.dumps(audit_payload)
                        )
                    )
                conn.commit()
        except Exception as e:
            ai_logger.warning(f"Could not persist moderation audit record to DB: {e}")
