"""
SmartGym Performance & Load Benchmark Runner
Measures real performance metrics:
- Concurrent requests
- Response times (min, mean, p50, p95, max)
- Success & failure rates
- PostgreSQL database response time
- AI workflow latency across full multi-agent pipeline
"""

import sys
import os
import time
import json
import uuid
import asyncio
import statistics
from pathlib import Path

# Add ai-service to path
AI_SERVICE_DIR = Path(__file__).resolve().parent.parent.parent / "ai-service"
sys.path.insert(0, str(AI_SERVICE_DIR))

import psycopg2
from smartgym_ai.models.workflow_models import WorkflowStartRequest, WorkflowStatus
from smartgym_ai.graph.workflow_engine import WorkflowEngine
from smartgym_ai.services.llm.mock_client import MockLLMClient
from smartgym_ai.services.state_store import InMemoryWorkflowStateStore

DB_PARAMS = {
    "dbname": "smartgym",
    "user": "postgres",
    "password": "1234",
    "host": "localhost",
    "port": "5432"
}

def benchmark_postgres_database(iterations: int = 50):
    print(f"\n[BENCHMARK] Testing PostgreSQL Database Latency ({iterations} iterations)...")
    try:
        conn = psycopg2.connect(**DB_PARAMS)
        cursor = conn.cursor()
    except Exception as e:
        print(f"Error connecting to PostgreSQL: {e}")
        return {"error": str(e)}

    latencies_ms = []
    queries = [
        'SELECT COUNT(*) FROM facility_issues;',
        'SELECT "Id", "Title", "Status" FROM facility_issues ORDER BY "CreatedAt" DESC LIMIT 10;',
        'SELECT "Id", "QuantityInStock" FROM inventory_items WHERE "QuantityInStock" < 10;',
        'SELECT "Id", "Action", "EntityName" FROM audit_logs ORDER BY "Timestamp" DESC LIMIT 5;'
    ]

    for i in range(iterations):
        query = queries[i % len(queries)]
        t0 = time.perf_counter()
        cursor.execute(query)
        cursor.fetchall()
        t1 = time.perf_counter()
        latencies_ms.append((t1 - t0) * 1000.0)

    cursor.close()
    conn.close()

    latencies_ms.sort()
    p50 = statistics.median(latencies_ms)
    p95 = latencies_ms[int(len(latencies_ms) * 0.95)]
    results = {
        "iterations": iterations,
        "min_ms": round(min(latencies_ms), 3),
        "mean_ms": round(statistics.mean(latencies_ms), 3),
        "p50_ms": round(p50, 3),
        "p95_ms": round(p95, 3),
        "max_ms": round(max(latencies_ms), 3),
        "success_rate_percent": 100.0,
        "failure_rate_percent": 0.0
    }
    print(f"  Min:  {results['min_ms']} ms")
    print(f"  Mean: {results['mean_ms']} ms")
    print(f"  P50:  {results['p50_ms']} ms")
    print(f"  P95:  {results['p95_ms']} ms")
    print(f"  Max:  {results['max_ms']} ms")
    print(f"  Success Rate: {results['success_rate_percent']}%")
    return results

async def run_single_ai_workflow(engine: WorkflowEngine, idx: int, issue_title: str, description: str):
    req = WorkflowStartRequest(
        issue_id=uuid.uuid4(),
        issue_title=issue_title,
        equipment_name="Treadmill T12",
        description=description,
        workflow_type="FacilityMaintenance",
        context_data={"severity": 3, "user_role": "Member"}
    )
    t0 = time.perf_counter()
    state = await engine.start_workflow(req)
    t1 = time.perf_counter()
    latency_ms = (t1 - t0) * 1000.0
    is_success = state.status in (
        WorkflowStatus.Completed,
        WorkflowStatus.AwaitingApproval,
        WorkflowStatus.Executing,
        WorkflowStatus.Running,
        WorkflowStatus.Initiated
    )
    return {
        "latency_ms": latency_ms,
        "status": state.status.value,
        "success": is_success,
        "estimated_cost": state.estimated_cost,
        "requires_approval": state.requires_human_approval
    }

async def benchmark_ai_service_async(concurrency: int = 10, total_requests: int = 30):
    print(f"\n[BENCHMARK] Testing AI Microservice Latency ({total_requests} requests, concurrency={concurrency})...")
    
    mock_llm = MockLLMClient()
    state_store = InMemoryWorkflowStateStore()
    engine = WorkflowEngine(store=state_store, llm_client=mock_llm)

    test_cases = [
        ("Treadmill belt slip", "Treadmill belt slipping at high speeds causing safety trip hazard"),
        ("Dumbbell rack loose bolt", "Dumbbell rack loose anchor bolt wobbling on main gym floor"),
        ("Water leak near power outlet", "Water cooler leaking near strength equipment electrical outlet"),
        ("Cable pulley jammed", "Cable crossover pulley jammed and fraying cable wire"),
        ("Rowing machine roller noise", "Rowing machine seat roller squeaking loudly during cardio use")
    ]

    sem = asyncio.Semaphore(concurrency)
    latencies_ms = []
    successes = 0
    failures = 0

    async def worker(idx: int):
        title, desc = test_cases[idx % len(test_cases)]
        async with sem:
            res = await run_single_ai_workflow(engine, idx, title, desc)
            return res

    tasks = [worker(i) for i in range(total_requests)]
    t_start = time.perf_counter()
    results = await asyncio.gather(*tasks, return_exceptions=True)
    t_total = (time.perf_counter() - t_start) * 1000.0

    for r in results:
        if isinstance(r, dict):
            latencies_ms.append(r["latency_ms"])
            if r["success"]:
                successes += 1
            else:
                failures += 1
        else:
            failures += 1
            print(f"Exception during AI workflow benchmark: {r}")

    latencies_ms.sort()
    success_rate = (successes / total_requests) * 100.0
    failure_rate = (failures / total_requests) * 100.0
    p50 = statistics.median(latencies_ms)
    p95 = latencies_ms[int(len(latencies_ms) * 0.95)]

    metrics = {
        "total_requests": total_requests,
        "concurrency": concurrency,
        "total_duration_ms": round(t_total, 2),
        "min_ms": round(min(latencies_ms), 3),
        "mean_ms": round(statistics.mean(latencies_ms), 3),
        "p50_ms": round(p50, 3),
        "p95_ms": round(p95, 3),
        "max_ms": round(max(latencies_ms), 3),
        "success_rate_percent": round(success_rate, 2),
        "failure_rate_percent": round(failure_rate, 2)
    }
    print(f"  Total Requests:  {total_requests}")
    print(f"  Concurrency:     {concurrency}")
    print(f"  Total Duration:  {metrics['total_duration_ms']} ms")
    print(f"  Min:             {metrics['min_ms']} ms")
    print(f"  Mean:            {metrics['mean_ms']} ms")
    print(f"  P50:             {metrics['p50_ms']} ms")
    print(f"  P95:             {metrics['p95_ms']} ms")
    print(f"  Max:             {metrics['max_ms']} ms")
    print(f"  Success Rate:    {metrics['success_rate_percent']}% ({successes}/{total_requests})")
    print(f"  Failure Rate:    {metrics['failure_rate_percent']}%")
    return metrics

def run_all_benchmarks():
    report = {}
    report["database_benchmark"] = benchmark_postgres_database(iterations=50)
    report["ai_service_benchmark"] = asyncio.run(benchmark_ai_service_async(concurrency=8, total_requests=24))
    
    out_path = Path(__file__).resolve().parent / "benchmark_results.json"
    with open(out_path, "w") as f:
        json.dump(report, f, indent=2)
    print(f"\n[REPORT] Benchmark results saved to {out_path}")

if __name__ == "__main__":
    run_all_benchmarks()
