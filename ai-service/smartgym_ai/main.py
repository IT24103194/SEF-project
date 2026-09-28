import logging
import time
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from smartgym_ai.configuration.settings import settings

# Configure structured logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s"
)
logger = logging.getLogger("smartgym-ai")

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

@app.middleware("http")
async def log_requests(request: Request, call_next):
    start_time = time.time()
    response = await call_next(request)
    duration_ms = (time.time() - start_time) * 1000
    logger.info(f"{request.method} {request.url.path} responded {response.status_code} in {duration_ms:.2f}ms")
    return response

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
