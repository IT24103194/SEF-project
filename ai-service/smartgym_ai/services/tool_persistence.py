import json
from typing import Optional, Any
from uuid import UUID, uuid4
from datetime import datetime, timezone
import psycopg2

from smartgym_ai.configuration.settings import settings
from smartgym_ai.services.logging import ai_logger, get_correlation_id


class ToolExecutionPersistenceService:
    """
    Persists observable tool execution records to the PostgreSQL 'ai_tool_executions' table.
    Enables complete auditing and telemetry of all agent tool usage.
    """

    def __init__(self, db_url: Optional[str] = None):
        self.db_url = db_url or settings.DATABASE_URL

    def _get_connection(self):
        return psycopg2.connect(self.db_url)

    def record_execution(
        self,
        tool_name: str,
        input_params: Optional[dict[str, Any]] = None,
        output_result: Any = None,
        is_success: bool = True,
        execution_time_ms: int = 0,
        workflow_step_id: Optional[UUID] = None,
        **kwargs: Any
    ) -> Optional[UUID]:
        """Convenience method matching agent call signature."""
        params = input_params if input_params is not None else kwargs.get("input_parameters", {})
        step_id = workflow_step_id or kwargs.get("step_id")
        return self.persist_execution(
            tool_name=tool_name,
            input_parameters=params,
            output_result=output_result,
            is_success=is_success,
            execution_time_ms=execution_time_ms,
            step_id=step_id
        )

    def persist_execution(
        self,
        tool_name: str,
        input_parameters: dict[str, Any],
        output_result: Any,
        is_success: bool,
        execution_time_ms: int,
        step_id: Optional[UUID] = None
    ) -> Optional[UUID]:
        if not self.db_url:
            return None

        execution_id = uuid4()
        now = datetime.now(timezone.utc)

        input_json = json.dumps(input_parameters, default=str)
        output_json = json.dumps(output_result, default=str) if output_result is not None else None

        query = """
        INSERT INTO "ai_tool_executions" (
            "Id", "WorkflowStepId", "ToolName", "InputParametersJson",
            "OutputResultJson", "IsSuccess", "ExecutionTimeMs", "ExecutedAt"
        ) VALUES (
            %s, %s, %s, %s, %s, %s, %s, %s
        )
        """

        target_step_id = None
        try:
            with self._get_connection() as conn:
                with conn.cursor() as cur:
                    if step_id:
                        # 1. Check if step_id is a direct ai_workflow_steps.Id
                        cur.execute('SELECT "Id" FROM "ai_workflow_steps" WHERE "Id" = %s', (str(step_id),))
                        row = cur.fetchone()
                        if row:
                            target_step_id = str(row[0])
                        else:
                            # 2. Check if step_id is a WorkflowId that has steps
                            cur.execute('SELECT "Id" FROM "ai_workflow_steps" WHERE "WorkflowId" = %s LIMIT 1', (str(step_id),))
                            row = cur.fetchone()
                            if row:
                                target_step_id = str(row[0])
                            else:
                                # 3. Check if step_id is in ai_workflows; if so, create a step record
                                cur.execute('SELECT "Id" FROM "ai_workflows" WHERE "Id" = %s', (str(step_id),))
                                wf_row = cur.fetchone()
                                if wf_row:
                                    step_uuid = str(uuid4())
                                    cur.execute(
                                        'INSERT INTO "ai_workflow_steps" ("Id", "WorkflowId", "StepOrder", "StepName", "Status", "Summary", "ExecutionDurationMs", "ExecutedAt") '
                                        'VALUES (%s, %s, 1, %s, %s, %s, %s, %s)',
                                        (step_uuid, str(wf_row[0]), 99, f"Tool: {tool_name}", "Completed", f"Executed {tool_name}", execution_time_ms, now)
                                    )
                                    target_step_id = step_uuid

                    # If still no target_step_id, find any existing step in ai_workflow_steps as parent
                    if not target_step_id:
                        cur.execute('SELECT "Id" FROM "ai_workflow_steps" ORDER BY "ExecutedAt" DESC LIMIT 1')
                        any_row = cur.fetchone()
                        if any_row:
                            target_step_id = str(any_row[0])

                    if not target_step_id:
                        ai_logger.info("Skipping tool persistence: No parent workflow step available in PostgreSQL.")
                        return None

                    cur.execute(
                        query,
                        (
                            str(execution_id),
                            target_step_id,
                            tool_name,
                            input_json,
                            output_json,
                            is_success,
                            execution_time_ms,
                            now
                        )
                    )
                conn.commit()

            ai_logger.info(
                f"Persisted tool execution {execution_id} for '{tool_name}' ({execution_time_ms}ms, success={is_success})",
                extra={
                    "correlation_id": get_correlation_id(),
                    "extra_data": {
                        "execution_id": str(execution_id),
                        "tool_name": tool_name,
                        "is_success": is_success,
                        "duration_ms": execution_time_ms
                    }
                }
            )
            return execution_id
        except Exception as e:
            ai_logger.warning(
                f"Failed to persist tool execution to PostgreSQL for '{tool_name}': {e}",
                extra={"correlation_id": get_correlation_id()}
            )
            return None

    def get_executions_for_tool(self, tool_name: str, limit: int = 20) -> list[dict[str, Any]]:
        """Retrieve historical tool executions from database for verification and tests."""
        if not self.db_url:
            return []

        query = """
        SELECT "Id", "ToolName", "InputParametersJson", "OutputResultJson",
               "IsSuccess", "ExecutionTimeMs", "ExecutedAt"
        FROM "ai_tool_executions"
        WHERE "ToolName" = %s
        ORDER BY "ExecutedAt" DESC
        LIMIT %s
        """

        try:
            with self._get_connection() as conn:
                with conn.cursor() as cur:
                    cur.execute(query, (tool_name, limit))
                    rows = cur.fetchall()
                    results = []
                    for row in rows:
                        results.append({
                            "id": row[0],
                            "tool_name": row[1],
                            "input_parameters": row[2] if isinstance(row[2], dict) else json.loads(row[2] or "{}"),
                            "output_result": row[3] if isinstance(row[3], dict) else (json.loads(row[3]) if row[3] else None),
                            "is_success": row[4],
                            "execution_time_ms": row[5],
                            "executed_at": row[6]
                        })
                    return results
        except Exception as e:
            ai_logger.warning(f"Failed to query tool executions for '{tool_name}': {e}")
            return []


# Global singleton instance
tool_persistence_service = ToolExecutionPersistenceService()
