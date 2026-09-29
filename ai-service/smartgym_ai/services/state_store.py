import threading
from abc import ABC, abstractmethod
from typing import Optional
from uuid import UUID
from smartgym_ai.models.workflow_models import WorkflowState, WorkflowStatus


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
            # Order newest first
            results.sort(key=lambda w: w.started_at, reverse=True)
            return [w.model_copy(deep=True) for w in results[:limit]]

    def clear(self) -> None:
        """Clear all states (used for test teardown)."""
        with self._lock:
            self._storage.clear()


# Default singleton instance for the service runtime
workflow_store = InMemoryWorkflowStateStore()
