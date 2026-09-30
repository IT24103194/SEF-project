import json
import threading
from abc import ABC, abstractmethod
from typing import Optional
from uuid import UUID, uuid4
from datetime import datetime, timezone
import psycopg2
from psycopg2.extras import Json

from smartgym_ai.models.workflow_models import (
    WorkflowState,
    WorkflowStatus,
    WorkflowStepState,
    WorkflowStepStatus,
    ToolExecutionRecord,
    ValidationResultRecord,
)
from smartgym_ai.configuration.settings import settings
from smartgym_ai.services.logging import ai_logger, get_correlation_id

# Mapping between Python WorkflowStatus enum and ASP.NET Core AIWorkflowStatus integer
STATUS_TO_INT: dict[WorkflowStatus, int] = {
    WorkflowStatus.Initiated: 1,
    WorkflowStatus.Planning: 2,
    WorkflowStatus.AwaitingApproval: 5,
    WorkflowStatus.Executing: 6,
    WorkflowStatus.Running: 6,
    WorkflowStatus.Approved: 6,
    WorkflowStatus.Completed: 7,
    WorkflowStatus.Failed: 8,
    WorkflowStatus.Cancelled: 8,
    WorkflowStatus.Rejected: 8,
}

INT_TO_STATUS: dict[int, WorkflowStatus] = {
    1: WorkflowStatus.Initiated,
    2: WorkflowStatus.Planning,
    5: WorkflowStatus.AwaitingApproval,
    6: WorkflowStatus.Executing,
    7: WorkflowStatus.Completed,
    8: WorkflowStatus.Failed,
}


class IWorkflowStateStore(ABC):
    """Abstract interface for storing and retrieving workflow execution state."""

    @abstractmethod
    def create(self, state: WorkflowState) -> WorkflowState:
        """Persist a newly initiated workflow state."""
        pass

    @abstractmethod
    def get(self, workflow_id: UUID) -> Optional[WorkflowState]:
        """Retrieve workflow state by its unique ID."""
        pass

    @abstractmethod
    def update(self, state: WorkflowState) -> WorkflowState:
        """Update existing workflow state."""
        pass

    @abstractmethod
    def list(self, status: Optional[WorkflowStatus] = None, limit: int = 50) -> list[WorkflowState]:
        """List workflows, optionally filtering by status."""
        pass


class InMemoryWorkflowStateStore(IWorkflowStateStore):
    """
    Thread-safe in-memory state repository for fast internal workflow state management,
    testing, and execution recovery.
    """

    def __init__(self):
        self._lock = threading.Lock()
        self._storage: dict[UUID, WorkflowState] = {}

    def create(self, state: WorkflowState) -> WorkflowState:
        with self._lock:
            self._storage[state.id] = state.model_copy(deep=True)
            return self._storage[state.id]

    def get(self, workflow_id: UUID) -> Optional[WorkflowState]:
        with self._lock:
            state = self._storage.get(workflow_id)
            return state.model_copy(deep=True) if state else None

    def update(self, state: WorkflowState) -> WorkflowState:
        with self._lock:
            if state.id not in self._storage:
                raise KeyError(f"Workflow state with ID {state.id} does not exist.")
            self._storage[state.id] = state.model_copy(deep=True)
            return self._storage[state.id]

    def list(self, status: Optional[WorkflowStatus] = None, limit: int = 50) -> list[WorkflowState]:
        with self._lock:
            results = list(self._storage.values())
            if status is not None:
                results = [w for w in results if w.status == status]
            results.sort(key=lambda w: w.started_at, reverse=True)
            return [w.model_copy(deep=True) for w in results[:limit]]

    def clear(self) -> None:
        """Clear all states (used for test teardown)."""
        with self._lock:
            self._storage.clear()


class PostgresWorkflowStateStore(IWorkflowStateStore):
    """
    Production PostgreSQL state persistence store.
    Stores complete workflow definitions and planned/executed steps into
    the 'ai_workflows' and 'ai_workflow_steps' tables.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL or "postgresql://postgres:1234@localhost:5432/smartgym"

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    def create(self, state: WorkflowState) -> WorkflowState:
        now = datetime.now(timezone.utc)
        status_int = STATUS_TO_INT.get(state.status, 1)

        payload_json = None
        if state.structured_output:
            payload_json = json.dumps(state.structured_output, default=str)

        workflow_query = """
        INSERT INTO "ai_workflows" (
            "Id", "IssueId", "WorkflowType", "Status", "CurrentStep",
            "DiagnosisSummary", "RecommendedAction", "EstimatedConfidenceScore",
            "RequiresHumanApproval", "HumanApprovalGranted", "TotalTokensUsed",
            "ModelIdentifier", "StructuredOutputPayloadJson",
            "StartedAt", "CompletedAt", "CreatedAt", "UpdatedAt"
        ) VALUES (
            %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s
        )
        ON CONFLICT ("Id") DO UPDATE SET
            "Status" = EXCLUDED."Status",
            "CurrentStep" = EXCLUDED."CurrentStep",
            "DiagnosisSummary" = EXCLUDED."DiagnosisSummary",
            "RecommendedAction" = EXCLUDED."RecommendedAction",
            "EstimatedConfidenceScore" = EXCLUDED."EstimatedConfidenceScore",
            "RequiresHumanApproval" = EXCLUDED."RequiresHumanApproval",
            "HumanApprovalGranted" = EXCLUDED."HumanApprovalGranted",
            "TotalTokensUsed" = EXCLUDED."TotalTokensUsed",
            "StructuredOutputPayloadJson" = EXCLUDED."StructuredOutputPayloadJson",
            "CompletedAt" = EXCLUDED."CompletedAt",
            "UpdatedAt" = EXCLUDED."UpdatedAt";
        """

        step_query = """
        INSERT INTO "ai_workflow_steps" (
            "Id", "WorkflowId", "StepOrder", "StepName", "Status", "Summary",
            "ExecutionDurationMs", "ExecutedAt"
        ) VALUES (
            %s, %s, %s, %s, %s, %s, %s, %s
        )
        ON CONFLICT ("Id") DO UPDATE SET
            "StepOrder" = EXCLUDED."StepOrder",
            "StepName" = EXCLUDED."StepName",
            "Status" = EXCLUDED."Status",
            "Summary" = EXCLUDED."Summary",
            "ExecutionDurationMs" = EXCLUDED."ExecutionDurationMs",
            "ExecutedAt" = EXCLUDED."ExecutedAt";
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    workflow_query,
                    (
                        str(state.id),
                        str(state.issue_id),
                        state.workflow_type,
                        status_int,
                        state.current_step,
                        state.diagnosis_summary,
                        state.recommended_action,
                        state.estimated_confidence_score,
                        state.requires_human_approval,
                        state.human_approval_granted,
                        state.total_tokens_used,
                        state.model_identifier,
                        payload_json,
                        state.started_at,
                        state.completed_at,
                        state.started_at,
                        now,
                    )
                )

                # Persist all individual planned / executed steps
                for step in state.steps:
                    cur.execute(
                        step_query,
                        (
                            str(step.id),
                            str(state.id),
                            step.step_order,
                            step.step_name,
                            step.status.value if hasattr(step.status, "value") else str(step.status),
                            step.summary,
                            step.execution_duration_ms,
                            step.executed_at or now,
                        )
                    )
            conn.commit()

        ai_logger.info(
            f"Successfully persisted workflow {state.id} and {len(state.steps)} steps to PostgreSQL.",
            extra={
                "correlation_id": state.correlation_id or get_correlation_id(),
                "workflow_id": str(state.id),
                "extra_data": {"status": state.status, "total_steps": len(state.steps)}
            }
        )
        return state

    def update(self, state: WorkflowState) -> WorkflowState:
        return self.create(state)

    def get(self, workflow_id: UUID) -> Optional[WorkflowState]:
        workflow_query = """
        SELECT
            "Id", "IssueId", "WorkflowType", "Status", "CurrentStep",
            "DiagnosisSummary", "RecommendedAction", "EstimatedConfidenceScore",
            "RequiresHumanApproval", "HumanApprovalGranted", "TotalTokensUsed",
            "ModelIdentifier", "StructuredOutputPayloadJson", "StartedAt", "CompletedAt"
        FROM "ai_workflows"
        WHERE "Id" = %s
        """

        steps_query = """
        SELECT
            "Id", "StepName", "StepOrder", "Status", "Summary",
            "ExecutionDurationMs", "ExecutedAt"
        FROM "ai_workflow_steps"
        WHERE "WorkflowId" = %s
        ORDER BY "StepOrder" ASC
        """

        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(workflow_query, (str(workflow_id),))
                row = cur.fetchone()
                if not row:
                    return None

                (
                    w_id, issue_id, wf_type, status_int, current_step,
                    diag_summary, rec_action, conf_score, req_app, app_granted,
                    tokens, model_id, payload_raw, started_at, completed_at
                ) = row

                cur.execute(steps_query, (str(workflow_id),))
                step_rows = cur.fetchall()

                steps = []
                for s_row in step_rows:
                    steps.append(
                        WorkflowStepState(
                            id=UUID(s_row[0]),
                            step_name=s_row[1],
                            step_order=s_row[2],
                            status=WorkflowStepStatus(s_row[3]) if s_row[3] in WorkflowStepStatus._value2member_map_ else WorkflowStepStatus.Pending,
                            summary=s_row[4] or "",
                            execution_duration_ms=s_row[5] or 0,
                            executed_at=s_row[6]
                        )
                    )

                status_enum = INT_TO_STATUS.get(status_int, WorkflowStatus.Running)
                payload_dict = None
                if payload_raw:
                    payload_dict = json.loads(payload_raw) if isinstance(payload_raw, str) else payload_raw

                return WorkflowState(
                    id=UUID(w_id),
                    issue_id=UUID(issue_id),
                    workflow_type=wf_type,
                    status=status_enum,
                    current_step=current_step,
                    diagnosis_summary=diag_summary or "",
                    recommended_action=rec_action or "",
                    estimated_confidence_score=conf_score or 0.0,
                    requires_human_approval=req_app or False,
                    human_approval_granted=app_granted,
                    total_tokens_used=tokens or 0,
                    model_identifier=model_id or "smartgym-ai",
                    structured_output=payload_dict,
                    steps=steps,
                    started_at=started_at,
                    completed_at=completed_at
                )

    def list(self, status: Optional[WorkflowStatus] = None, limit: int = 50) -> list[WorkflowState]:
        query = """
        SELECT "Id" FROM "ai_workflows"
        """
        params = []
        if status is not None:
            query += ' WHERE "Status" = %s'
            params.append(STATUS_TO_INT.get(status, 1))

        query += ' ORDER BY "StartedAt" DESC LIMIT %s'
        params.append(limit)

        results = []
        with self._get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(query, tuple(params))
                ids = [UUID(r[0]) for r in cur.fetchall()]

        for w_id in ids:
            w = self.get(w_id)
            if w:
                results.append(w)
        return results


class HybridWorkflowStateStore(IWorkflowStateStore):
    """
    Hybrid state store: maintains fast thread-safe in-memory cache and
    asynchronously synchronizes to PostgreSQL when database connectivity is present.
    """

    def __init__(self, memory_store: Optional[InMemoryWorkflowStateStore] = None, postgres_store: Optional[PostgresWorkflowStateStore] = None):
        self.memory = memory_store or InMemoryWorkflowStateStore()
        self.postgres = postgres_store or PostgresWorkflowStateStore()

    def create(self, state: WorkflowState) -> WorkflowState:
        saved = self.memory.create(state)
        try:
            self.postgres.create(state)
        except Exception as e:
            ai_logger.warning(
                f"PostgreSQL persistence skipped/failed: {str(e)}. In-memory state retained.",
                extra={"correlation_id": get_correlation_id(), "workflow_id": str(state.id)}
            )
        return saved

    def get(self, workflow_id: UUID) -> Optional[WorkflowState]:
        cached = self.memory.get(workflow_id)
        if cached:
            return cached
        try:
            from_db = self.postgres.get(workflow_id)
            if from_db:
                self.memory.create(from_db)
                return from_db
        except Exception:
            pass
        return None

    def update(self, state: WorkflowState) -> WorkflowState:
        saved = self.memory.update(state)
        try:
            self.postgres.update(state)
        except Exception as e:
            ai_logger.warning(
                f"PostgreSQL update skipped/failed: {str(e)}",
                extra={"correlation_id": get_correlation_id(), "workflow_id": str(state.id)}
            )
        return saved

    def list(self, status: Optional[WorkflowStatus] = None, limit: int = 50) -> list[WorkflowState]:
        return self.memory.list(status=status, limit=limit)


# Default singleton instance using Hybrid persistence
workflow_store = HybridWorkflowStateStore()
