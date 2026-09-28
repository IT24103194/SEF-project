# ADR-001: Monorepo Architecture for Integrated Full-Stack & Agentic AI Application

## Status
Accepted

## Context
SmartGym requires a synchronized full-stack architecture comprising an ASP.NET Core Web API backend, a PostgreSQL relational database, a React web administration console, a Flutter mobile client, and a Python LangGraph multi-agent AI microservice. A key academic requirement of SE3090 Assignment 1 is that the system operates as one coherent, unified system rather than disconnected prototypes.

## Options Considered
1. **Multi-Repo Architecture**: Separate repositories for backend, AI service, web, and mobile.
2. **Unified Monorepo Architecture**: A single Git repository containing all components organized by clear directory boundaries with unified docker orchestration and root documentation.

## Decision
We chose the **Unified Monorepo Architecture**.

### Rationale:
- Enables atomic pull requests, cross-platform issue tracking, and single-source-of-truth documentation.
- Guarantees API contract alignment across ASP.NET Core DTOs, React TypeScript/JavaScript consumers, and Flutter models.
- Simplifies local multi-container development via a single `docker-compose.yml`.
- Directly satisfies academic viva requirements by demonstrating how each tier integrates without fragmented version drift.

## Consequences
- **Positive**: Single CI workflow in GitHub Actions, zero dependency version drift across contracts, unified Docker orchestration.
- **Negative**: Monorepo size requires careful `.gitignore` rules to avoid committing build artifacts from multiple distinct ecosystems (.NET `bin`/`obj`, Node `node_modules`, Python `.venv`, Flutter `.dart_tool`).
