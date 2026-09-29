import logging
import json
import uuid
from contextvars import ContextVar
from typing import Optional, Any
from datetime import datetime, timezone

# Context variable for request-level correlation tracking
correlation_id_ctx: ContextVar[Optional[str]] = ContextVar("correlation_id", default=None)
workflow_id_ctx: ContextVar[Optional[str]] = ContextVar("workflow_id", default=None)


def get_correlation_id() -> str:
    """Returns current correlation ID or generates a new fallback UUID."""
    cid = correlation_id_ctx.get()
    if not cid:
        cid = str(uuid.uuid4())
        correlation_id_ctx.set(cid)
    return cid


def set_correlation_id(correlation_id: str) -> None:
    """Sets correlation ID in current async context."""
    correlation_id_ctx.set(correlation_id)


def get_workflow_id() -> Optional[str]:
    """Returns current workflow ID if set in context."""
    return workflow_id_ctx.get()


def set_workflow_id(workflow_id: Optional[str]) -> None:
    """Sets workflow ID in current async context."""
    workflow_id_ctx.set(workflow_id)


class StructuredJsonFormatter(logging.Formatter):
    """
    Structured JSON log formatter for AI service events,
    incorporating correlation ID, workflow ID, and ISO timestamps.
    """

    def format(self, record: logging.LogRecord) -> str:
        log_data = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
            "service": "smartgym-ai",
            "correlation_id": getattr(record, "correlation_id", None) or correlation_id_ctx.get() or "none",
            "workflow_id": getattr(record, "workflow_id", None) or workflow_id_ctx.get() or "none",
        }

        # Include additional extra attributes if present
        if hasattr(record, "extra_data") and isinstance(record.extra_data, dict):
            # Sanitize any accidental secret leakage
            sanitized = {}
            for k, v in record.extra_data.items():
                if any(secret_term in k.lower() for secret_term in ["key", "secret", "token", "password", "auth"]):
                    sanitized[k] = "[REDACTED]"
                else:
                    sanitized[k] = v
            log_data["details"] = sanitized

        if record.exc_info:
            log_data["exception"] = self.formatException(record.exc_info)

        return json.dumps(log_data)


def configure_structured_logging(level: int = logging.INFO) -> logging.Logger:
    """Configures the root smartgym-ai logger with structured JSON output."""
    logger = logging.getLogger("smartgym-ai")
    logger.setLevel(level)

    # Avoid duplicate handlers on re-configuration
    if not logger.handlers:
        handler = logging.StreamHandler()
        handler.setFormatter(StructuredJsonFormatter())
        logger.addHandler(handler)

    logger.propagate = False
    return logger


ai_logger = configure_structured_logging()
