import time
import uuid
from uuid import UUID
from typing import Optional
from fastapi import FastAPI, Request, HTTPException, Security, Depends, status
from fastapi.security.api_key import APIKeyHeader
from fastapi.middleware.cors import CORSMiddleware

from smartgym_ai.configuration.settings import settings
from smartgym_ai.models.workflow_models import (
    WorkflowState,
    WorkflowStartRequest,
    WorkflowResumeRequest,
    WorkflowCancelRequest,
)
from smartgym_ai.graph.workflow_engine import workflow_engine
from smartgym_ai.services.logging import (
    ai_logger,
    set_correlation_id,
    get_correlation_id,
    set_workflow_id,
)

app = FastAPI(
    title="SmartGym Internal Agentic AI Service",
    version="1.0.0",
    description="Internal multi-agent orchestration microservice for SmartGym facility problem resolution."
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

api_key_header = APIKeyHeader(name=settings.INTERNAL_API_KEY_HEADER, auto_error=False)


async def verify_internal_api_key(api_key: Optional[str] = Security(api_key_header)) -> str:
    """
    Security gate ensuring only authorized internal callers (e.g. ASP.NET Core API)
    can trigger or query AI workflows. React and Flutter clients must never access directly.
    """
    if not api_key or api_key != settings.AI_SERVICE_API_KEY:
        ai_logger.warning(
            "Unauthorized attempt to access internal AI endpoint",
            extra={"correlation_id": get_correlation_id()}
        )
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Unauthorized: Valid internal API key required."
        )
    return api_key


@app.middleware("http")
async def correlation_and_logging_middleware(request: Request, call_next):
    # Extract or generate Correlation ID
    correlation_id = request.headers.get(settings.CORRELATION_ID_HEADER) or str(uuid.uuid4())
    set_correlation_id(correlation_id)

    start_time = time.perf_counter()
    response = await call_next(request)
    duration_ms = (time.perf_counter() - start_time) * 1000

    # Attach correlation ID to response
    response.headers[settings.CORRELATION_ID_HEADER] = correlation_id

    ai_logger.info(
        f"{request.method} {request.url.path} responded {response.status_code} in {duration_ms:.2f}ms",
        extra={
            "correlation_id": correlation_id,
            "extra_data": {
                "method": request.method,
                "path": request.url.path,
                "status_code": response.status_code,
                "duration_ms": round(duration_ms, 2)
            }
        }
    )
    return response


# --- Internal Workflow Endpoints ---

@app.post(
    "/internal/workflows/start",
    response_model=WorkflowState,
    status_code=status.HTTP_201_CREATED,
    tags=["Internal Workflows"],
    dependencies=[Depends(verify_internal_api_key)]
)
async def start_workflow(request: WorkflowStartRequest):
    """
    Start a new agentic AI workflow for an identified gym equipment issue.
    Accessible ONLY to internal backend services.
    """
    try:
        state = await workflow_engine.start_workflow(request)
        return state
    except Exception as e:
        ai_logger.error(
            f"Failed to start workflow for issue {request.issue_id}: {str(e)}",
            extra={"correlation_id": get_correlation_id()}
        )
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Workflow startup failed: {str(e)}"
        )


@app.get(
    "/internal/workflows/{workflow_id}",
    response_model=WorkflowState,
    tags=["Internal Workflows"],
    dependencies=[Depends(verify_internal_api_key)]
)
async def get_workflow(workflow_id: UUID):
    """
    Retrieve live workflow execution state, steps, and validation results.
    """
    state = workflow_engine.get_workflow(workflow_id)
    if not state:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"AI Workflow with ID '{workflow_id}' not found."
        )
    return state


@app.post(
    "/internal/workflows/{workflow_id}/resume",
    response_model=WorkflowState,
    tags=["Internal Workflows"],
    dependencies=[Depends(verify_internal_api_key)]
)
async def resume_workflow(workflow_id: UUID, request: WorkflowResumeRequest):
    """
    Resume an existing workflow that is awaiting human approval or review.
    """
    try:
        state = await workflow_engine.resume_workflow(workflow_id, request)
        return state
    except KeyError:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"AI Workflow with ID '{workflow_id}' not found."
        )
    except Exception as e:
        ai_logger.error(
            f"Failed to resume workflow {workflow_id}: {str(e)}",
            extra={"correlation_id": get_correlation_id()}
        )
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Workflow resumption failed: {str(e)}"
        )


@app.post(
    "/internal/workflows/{workflow_id}/cancel",
    response_model=WorkflowState,
    tags=["Internal Workflows"],
    dependencies=[Depends(verify_internal_api_key)]
)
async def cancel_workflow(workflow_id: UUID, request: Optional[WorkflowCancelRequest] = None):
    """
    Cancel an ongoing or awaiting workflow.
    """
    reason = request.reason if request else "Cancelled by internal service"
    try:
        state = await workflow_engine.cancel_workflow(workflow_id, reason=reason)
        return state
    except KeyError:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"AI Workflow with ID '{workflow_id}' not found."
        )


# --- Health & Diagnostic Endpoints ---

@app.get("/health", tags=["Diagnostic"])
async def health_check():
    """Health check endpoint consumed by ASP.NET Core backend and Docker orchestration."""
    return {
        "status": "healthy",
        "service": "smartgym-ai",
        "version": "1.0.0",
        "provider": settings.LLM_PROVIDER,
        "model": settings.LLM_MODEL,
        "hitl_approval_threshold": settings.AI_APPROVAL_COST_THRESHOLD
    }


@app.get("/api/info", tags=["Diagnostic"])
async def service_info():
    """Returns AI architecture details, agents, and configuration."""
    return {
        "application": "SmartGym Agentic AI Microservice",
        "version": "1.0.0",
        "architecture": "LangGraph Stateful Multi-Agent System",
        "agents": [
            {"name": "Planner Agent", "role": "Coordinates diagnosis and plans remediation stages"},
            {"name": "Safety & Business Validation Agent", "role": "Verifies gym warranty, safety rules, and compliance"},
            {"name": "Gym Domain Analysis Agent", "role": "Diagnoses equipment failure modes and estimates parts/labor cost"},
            {"name": "Action / Tool Agent", "role": "Executes supplier queries, drafts repair orders, and sends notifications"}
        ],
        "hitl_gate": {
            "threshold_currency": "LKR (Rs.)",
            "threshold_value": settings.AI_APPROVAL_COST_THRESHOLD,
            "status": "active"
        }
    }


@app.get("/", tags=["Diagnostic"])
async def root():
    return {
        "message": "SmartGym Internal Agentic AI Microservice is online.",
        "documentation": "/docs",
        "health": "/health"
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("smartgym_ai.main:app", host=settings.HOST, port=settings.PORT, reload=True)
