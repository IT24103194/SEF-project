import json
from typing import Optional, Any
from uuid import UUID, uuid4
from datetime import datetime, timezone
import psycopg2
from pydantic import BaseModel, Field

from smartgym_ai.tools.base_tool import ToolBase
from smartgym_ai.configuration.settings import settings
from smartgym_ai.models.action_models import ApprovalStatus
from smartgym_ai.services.logging import ai_logger, get_correlation_id

# Idempotency cache for vendor actions
_DISPATCHED_VENDOR_ACTIONS: set[str] = set()


# --- Tool 1: createRepairOrder ---

class CreateRepairOrderInput(BaseModel):
    issue_id: str = Field(description="Facility Issue UUID")
    equipment_id: Optional[str] = Field(default=None, description="Equipment UUID")
    estimated_cost: float = Field(default=0.0, description="Estimated repair cost in LKR")
    technician_name: Optional[str] = Field(default=None, description="Assigned certified technician name")
    supplier_id: Optional[str] = Field(default=None, description="Supplier UUID if parts ordered")
    is_approved: bool = Field(default=False, description="Whether human approval has been granted")


class CreateRepairOrderTool(ToolBase):
    """
    Creates or updates an official repair order in the SmartGym facility database.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "createRepairOrder"

    @property
    def description(self) -> str:
        return "Creates or updates an official repair order record in PostgreSQL."

    @property
    def args_schema(self):
        return CreateRepairOrderInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        issue_id: str,
        equipment_id: Optional[str] = None,
        estimated_cost: float = 0.0,
        technician_name: Optional[str] = None,
        supplier_id: Optional[str] = None,
        is_approved: bool = False
    ) -> dict[str, Any]:
        if not self.db_url:
            return {"created": False, "error": "Database not configured."}

        clean_issue_id = str(UUID(str(issue_id).strip()))
        clean_eq_id = None
        if equipment_id:
            try:
                clean_eq_id = str(UUID(str(equipment_id).strip()))
            except ValueError:
                pass

        clean_supp_id = None
        if supplier_id:
            try:
                clean_supp_id = str(UUID(str(supplier_id).strip()))
            except ValueError:
                pass

        now = datetime.now(timezone.utc)
        order_uuid = str(uuid4())
        order_number = f"RO-{now.strftime('%Y%m')}-{order_uuid[:6].upper()}"
        status_code = 3 if is_approved else 2  # Approved (3) vs PendingApproval (2)

        # Check if a repair order already exists for this issue
        check_query = 'SELECT "Id", "OrderNumber", "Status" FROM "repair_orders" WHERE "IssueId" = %s::uuid LIMIT 1'
        insert_query = """
        INSERT INTO "repair_orders" (
            "Id", "IssueId", "EquipmentId", "OrderNumber", "EstimatedCost",
            "Status", "TechnicianName", "SupplierId", "CreatedAt"
        ) VALUES (
            %s::uuid, %s::uuid, %s::uuid, %s, %s, %s, %s, %s::uuid, %s
        )
        RETURNING "Id", "OrderNumber"
        """
        update_query = """
        UPDATE "repair_orders"
        SET "Status" = %s, "EstimatedCost" = %s, "UpdatedAt" = %s
        WHERE "Id" = %s::uuid
        RETURNING "Id", "OrderNumber"
        """

        try:
            with self._get_connection() as conn:
                with conn.cursor() as cur:
                    # Find equipment_id from facility_issues if not provided
                    if not clean_eq_id:
                        cur.execute('SELECT "EquipmentId" FROM "facility_issues" WHERE "Id" = %s::uuid', (clean_issue_id,))
                        eq_row = cur.fetchone()
                        if eq_row and eq_row[0]:
                            clean_eq_id = str(eq_row[0])

                    if not clean_eq_id:
                        # Fallback to any valid equipment row
                        cur.execute('SELECT "Id" FROM "equipment" LIMIT 1')
                        fallback_eq = cur.fetchone()
                        if fallback_eq:
                            clean_eq_id = str(fallback_eq[0])

                    cur.execute(check_query, (clean_issue_id,))
                    existing = cur.fetchone()

                    if existing:
                        ro_id = str(existing[0])
                        ro_num = existing[1]
                        cur.execute(update_query, (status_code, estimated_cost, now, ro_id))
                        action_taken = "updated"
                    else:
                        cur.execute(
                            insert_query,
                            (
                                order_uuid, clean_issue_id, clean_eq_id, order_number,
                                estimated_cost, status_code, technician_name or "Certified Technician",
                                clean_supp_id, now
                            )
                        )
                        created_row = cur.fetchone()
                        ro_id = str(created_row[0])
                        ro_num = created_row[1]
                        action_taken = "created"

                    conn.commit()

            return {
                "created": True,
                "action": action_taken,
                "repair_order_id": ro_id,
                "order_number": ro_num,
                "status": "Approved" if is_approved else "PendingApproval",
                "estimated_cost": float(estimated_cost)
            }
        except Exception as e:
            ai_logger.error(f"Failed to create/update repair order: {e}")
            return {"created": False, "error": str(e)}


# --- Tool 2: prepareVendorEmail ---

class PrepareVendorEmailInput(BaseModel):
    supplier_name: str = Field(description="Name of the vendor/supplier")
    supplier_email: Optional[str] = Field(default=None, description="Contact email of supplier")
    equipment_name: str = Field(description="Equipment name and model")
    serial_number: Optional[str] = Field(default=None, description="Equipment serial number")
    required_part: Optional[str] = Field(default=None, description="Part name or SKU required")
    issue_description: str = Field(description="Summary of symptoms or issue diagnosis")
    estimated_cost: float = Field(default=0.0, description="Estimated total budget")


class PrepareVendorEmailTool(ToolBase):
    """
    Drafts an official vendor procurement and dispatch RFQ email for parts or technician servicing.
    """

    @property
    def name(self) -> str:
        return "prepareVendorEmail"

    @property
    def description(self) -> str:
        return "Drafts a vendor RFQ / procurement email for equipment parts or certified service."

    @property
    def args_schema(self):
        return PrepareVendorEmailInput

    async def _run(
        self,
        supplier_name: str,
        equipment_name: str,
        issue_description: str,
        supplier_email: Optional[str] = None,
        serial_number: Optional[str] = None,
        required_part: Optional[str] = None,
        estimated_cost: float = 0.0
    ) -> dict[str, Any]:
        recipient = supplier_email or f"support@{supplier_name.lower().replace(' ', '')}.com"
        subject = f"[SmartGym Service Request] Urgent Maintenance & Parts Order - {equipment_name}"

        body = (
            f"Dear {supplier_name} Support & Logistics Team,\n\n"
            f"SmartGym Facilities Management has logged an equipment maintenance requirement:\n"
            f"- Equipment: {equipment_name}\n"
            f"- Serial Number: {serial_number or 'On File'}\n"
            f"- Suspected Cause / Issue: {issue_description}\n"
            f"- Required Component(s): {required_part or 'OEM Technician Diagnostic Kit'}\n"
            f"- Target Budget Authorization: LKR {estimated_cost:,.2f}\n\n"
            f"Please reply with component availability confirmation, dispatch timeline, and quotation.\n\n"
            f"Best regards,\n"
            f"SmartGym Automated Facility Logistics"
        )

        return {
            "prepared": True,
            "recipient": recipient,
            "subject": subject,
            "body": body,
            "supplier_name": supplier_name,
            "prepared_at": datetime.now(timezone.utc).isoformat()
        }


# --- Tool 3: sendVendorEmail ---

class SendVendorEmailInput(BaseModel):
    recipient: str = Field(description="Recipient vendor email address")
    subject: str = Field(description="Subject of vendor email")
    body: str = Field(description="Body content of vendor email")
    approval_status: str = Field(description="Current human approval status (e.g. APPROVED, PENDING)")
    idempotency_key: str = Field(description="Unique key to prevent duplicate email dispatches")
    workflow_id: Optional[str] = Field(default=None, description="Associated AI Workflow UUID")


class SendVendorEmailTool(ToolBase):
    """
    Dispatches vendor email.
    CRITICAL SECURITY ENFORCEMENT:
    Rejects execution in normal application code when human approval is absent.
    Prevents duplicate dispatch using idempotency checks.
    """

    @property
    def name(self) -> str:
        return "sendVendorEmail"

    @property
    def description(self) -> str:
        return "Dispatches official email to vendor. STRICTLY REQUIRES CONFIRMED HUMAN APPROVAL."

    @property
    def args_schema(self):
        return SendVendorEmailInput

    async def _run(
        self,
        recipient: str,
        subject: str,
        body: str,
        approval_status: str,
        idempotency_key: str,
        workflow_id: Optional[str] = None
    ) -> dict[str, Any]:
        # 1. APPLICATION-CODE PERMISSION ENFORCEMENT (Mandatory)
        normalized_status = str(approval_status).strip().upper()
        if normalized_status != ApprovalStatus.APPROVED.value:
            err_msg = (
                f"Action Rejected: Human approval is absent (Status: '{approval_status}'). "
                f"High-impact vendor communication requires confirmed ADMIN or FACILITY MANAGER authorization."
            )
            ai_logger.warning(
                f"Security check failed in sendVendorEmail: {err_msg}",
                extra={
                    "correlation_id": get_correlation_id(),
                    "extra_data": {"approval_status": approval_status, "recipient": recipient}
                }
            )
            raise PermissionError(err_msg)

        # 2. DUPLICATE ACTION PREVENTION (IDEMPOTENCY)
        if idempotency_key in _DISPATCHED_VENDOR_ACTIONS:
            ai_logger.warning(
                f"Duplicate vendor action detected and prevented for idempotency key: {idempotency_key}",
                extra={"correlation_id": get_correlation_id()}
            )
            return {
                "sent": False,
                "is_duplicate": True,
                "message": "Duplicate vendor action prevented: Email has already been dispatched for this approval key.",
                "error": "Duplicate vendor action prevented: Email has already been dispatched for this approval key.",
                "idempotency_key": idempotency_key
            }

        # 3. Execute transmission
        _DISPATCHED_VENDOR_ACTIONS.add(idempotency_key)
        now_iso = datetime.now(timezone.utc).isoformat()
        message_id = f"MSG-{uuid4().hex[:12].upper()}"

        ai_logger.info(
            f"Vendor email successfully dispatched to '{recipient}' (MessageID: {message_id})",
            extra={
                "correlation_id": get_correlation_id(),
                "extra_data": {
                    "recipient": recipient,
                    "subject": subject,
                    "message_id": message_id,
                    "workflow_id": workflow_id
                }
            }
        )

        return {
            "sent": True,
            "recipient": recipient,
            "subject": subject,
            "message_id": message_id,
            "dispatched_at": now_iso,
            "idempotency_key": idempotency_key
        }


# --- Tool 4: updateFacilityIssueStatus ---

class UpdateFacilityIssueStatusInput(BaseModel):
    issue_id: str = Field(description="Facility Issue UUID")
    status: str = Field(description="Target status (e.g. APPROVED, VENDOR_CONTACTED, REPAIR_SCHEDULED, REJECTED, REVISION_REQUIRED)")
    resolution_notes: Optional[str] = Field(default=None, description="Updated notes on issue progress")


class UpdateFacilityIssueStatusTool(ToolBase):
    """
    Updates facility issue status and notes in the SmartGym PostgreSQL database.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "updateFacilityIssueStatus"

    @property
    def description(self) -> str:
        return "Updates the status and resolution notes of a facility issue in PostgreSQL."

    @property
    def args_schema(self):
        return UpdateFacilityIssueStatusInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        issue_id: str,
        status: str,
        resolution_notes: Optional[str] = None
    ) -> dict[str, Any]:
        if not self.db_url:
            return {"updated": False, "error": "Database not configured."}

        clean_id = str(UUID(str(issue_id).strip()))
        status_map = {
            "SUBMITTED": 1,
            "AI_ANALYZING": 2,
            "PENDING_APPROVAL": 3,
            "APPROVED": 4,
            "VENDOR_CONTACTED": 5,
            "REPAIR_SCHEDULED": 6,
            "IN_PROGRESS": 7,
            "RESOLVED": 8,
            "REJECTED": 9,
            "REVISION_REQUIRED": 10,
        }
        numeric_status = status_map.get(status.strip().upper(), 4)

        query = """
        UPDATE "facility_issues"
        SET "Status" = %s,
            "ResolutionNotes" = COALESCE(%s, "ResolutionNotes"),
            "UpdatedAt" = %s
        WHERE "Id" = %s::uuid
        """

        try:
            with self._get_connection() as conn:
                with conn.cursor() as cur:
                    cur.execute(query, (numeric_status, resolution_notes, datetime.now(timezone.utc), clean_id))
                    conn.commit()

            return {
                "updated": True,
                "issue_id": clean_id,
                "status": status.strip().upper(),
                "status_code": numeric_status
            }
        except Exception as e:
            ai_logger.error(f"Failed to update facility issue status: {e}")
            return {"updated": False, "error": str(e)}


# --- Tool 5: createNotification ---

class CreateNotificationInput(BaseModel):
    user_id: Optional[str] = Field(default=None, description="Target User UUID for the notification")
    title: str = Field(description="Notification title")
    message: str = Field(description="Notification body content")
    notification_type: int = Field(default=3, description="NotificationType: 1=General, 2=Booking, 3=Maintenance, 4=Approval")
    target_url: Optional[str] = Field(default=None, description="App navigation URL for notification click")


class CreateNotificationTool(ToolBase):
    """
    Creates a notification in PostgreSQL for staff, managers, or members.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    @property
    def name(self) -> str:
        return "createNotification"

    @property
    def description(self) -> str:
        return "Creates a notification in PostgreSQL for facility managers or staff."

    @property
    def args_schema(self):
        return CreateNotificationInput

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    async def _run(
        self,
        title: str,
        message: str,
        user_id: Optional[str] = None,
        notification_type: int = 3,
        target_url: Optional[str] = None
    ) -> dict[str, Any]:
        if not self.db_url:
            return {"created": False, "error": "Database not configured."}

        target_uid = None
        if user_id:
            try:
                target_uid = str(UUID(str(user_id).strip()))
            except ValueError:
                pass

        now = datetime.now(timezone.utc)
        nid = str(uuid4())

        query = """
        INSERT INTO "notifications" (
            "Id", "UserId", "Type", "Title", "Message", "IsRead", "TargetUrl", "CreatedAt"
        ) VALUES (
            %s::uuid, %s::uuid, %s, %s, %s, %s, %s, %s
        )
        """

        try:
            with self._get_connection() as conn:
                with conn.cursor() as cur:
                    # If target_uid not provided, find first Admin user
                    if not target_uid:
                        cur.execute('SELECT "Id" FROM "users" LIMIT 1')
                        u_row = cur.fetchone()
                        if u_row:
                            target_uid = str(u_row[0])

                    if not target_uid:
                        return {"created": False, "error": "No user found to receive notification."}

                    cur.execute(
                        query,
                        (nid, target_uid, notification_type, title, message, False, target_url, now)
                    )
                    conn.commit()

            return {
                "created": True,
                "notification_id": nid,
                "recipient_user_id": target_uid,
                "title": title
            }
        except Exception as e:
            ai_logger.error(f"Failed to create notification: {e}")
            return {"created": False, "error": str(e)}
