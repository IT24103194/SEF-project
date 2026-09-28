# SmartGym Viva Defense & Academic Examination Guide

## 1. Core Architectural Questions & Answers

### Q1: Why use ASP.NET Core as the sole public gateway instead of letting React and Flutter connect directly to PostgreSQL or the AI service?
**Answer:** Centralizing requests through ASP.NET Core guarantees that all business rules, authorization policies, and input validations are enforced server-side. Direct database connections from client devices expose database ports, bypass business logic, and create major security vulnerabilities. Similarly, the Python AI microservice is an internal service that lacks public authentication mechanisms; ASP.NET Core validates client JWTs and enforces rate limits before delegating work to the AI agents.

### Q2: How does the Agentic AI workflow differ from a basic chatbot?
**Answer:** A generic chatbot merely performs one-shot prompt completion. SmartGym's AI subsystem is a stateful multi-agent system built on LangGraph comprising 4 distinct agents (Planner, Safety Validator, Gym Domain Analyst, Action Agent). It executes conditional routing, uses allow-listed tools, performs deterministic business validations (e.g. warranty and safety rules), and enforces a Human-in-the-Loop financial approval gate if estimated repair costs exceed Rs. 25,000.

### Q3: What is the Human-in-the-Loop (HITL) gate and why is it necessary?
**Answer:** Autonomous AI models can hallucinate or misjudge costs. To protect the business against unauthorized financial liability, the workflow pauses when repair estimates exceed Rs. 25,000, saving its state in PostgreSQL. A Facility Manager must explicitly approve or reject the repair order in the React Web Admin console before the Action Agent is permitted to dispatch supplier purchase requests.

### Q4: How is data consistency maintained across the system?
**Answer:** PostgreSQL 16 enforces ACID compliance with strict foreign key constraints. Entity Framework Core manages migrations, and all temporal data is stored in UTC (`timestamptz`).
