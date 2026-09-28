# SmartGym System Architecture & Component Topology

## 1. High-Level Architectural Diagram

```mermaid
graph TD
    subgraph Clients ["Presentation Tier"]
        ReactAdmin["React 18 Web Admin Console<br/>(Vite, Redux Toolkit, Custom CSS)"]
        FlutterMobile["Flutter 3 Mobile App<br/>(Dart, Riverpod, Secure Storage)"]
    end

    subgraph BackendAPI ["Authoritative Application Gateway"]
        AspNetCore["ASP.NET Core 8 Web API<br/>(RFC 7807, DI, JWT Auth, Swagger)"]
    end

    subgraph DataStore ["Persistence Tier"]
        PostgresDB[("PostgreSQL 16 Relational DB<br/>(34 Relational Entities via EF Core)")]
    end

    subgraph AgenticAI ["Internal Intelligence Tier"]
        AiService["Python FastAPI + LangGraph Service"]
        PlannerAgent["1. Coordinator / Planner Agent"]
        SafetyAgent["2. Safety & Business Validation Agent"]
        DomainAgent["3. Gym Domain Analysis Agent"]
        ActionAgent["4. Action / Tool Execution Agent"]
        
        AiService --> PlannerAgent
        PlannerAgent --> SafetyAgent
        SafetyAgent --> DomainAgent
        DomainAgent --> ActionAgent
    end

    subgraph External ["External Services"]
        LLMProvider["LLM Provider (OpenAI / Anthropic / Gemini / Mock)"]
        EmailService["Transactional Email API (Approved Requests)"]
    end

    ReactAdmin -->|"HTTPS / REST (JWT)"| AspNetCore
    FlutterMobile -->|"HTTPS / REST (JWT)"| AspNetCore
    AspNetCore -->|"EF Core 8 / Npgsql"| PostgresDB
    AspNetCore -->|"HTTP / REST (Internal Secret)"| AiService
    ActionAgent -->|"Prompt / Completion"| LLMProvider
    ActionAgent -->|"SMTP / API"| EmailService

    style ReactAdmin fill:#1e293b,stroke:#6366f1,stroke-width:2px,color:#fff
    style FlutterMobile fill:#1e293b,stroke:#06b6d4,stroke-width:2px,color:#fff
    style AspNetCore fill:#1e1b4b,stroke:#4f46e5,stroke-width:2px,color:#fff
    style PostgresDB fill:#064e3b,stroke:#10b981,stroke-width:2px,color:#fff
    style AiService fill:#451a03,stroke:#f59e0b,stroke-width:2px,color:#fff
```

## 2. Mandatory Architectural Constraints
1. **No Direct Database Access**: Neither React nor Flutter may establish direct TCP/SQL connections to PostgreSQL.
2. **No Direct AI Microservice Access**: The Python LangGraph AI microservice is strictly an internal service; public clients must route via ASP.NET Core controllers.
3. **No Direct Third-Party Access**: Supplier emails and order requests must be dispatched through server-side integrations managed by ASP.NET Core and the Action Agent.
4. **Stateful Human-in-the-Loop Gate**: Any repair recommendation exceeding **Rs. 25,000** must pause execution, persist state in PostgreSQL, and require an explicit authorization event from an Admin user in React before dispatching supplier orders.
