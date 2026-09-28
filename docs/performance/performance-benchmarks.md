# SmartGym Performance Benchmarks & Load Testing Plan

## 1. Objectives
Ensure the ASP.NET Core Web API, PostgreSQL database, and AI microservice satisfy enterprise latency targets under concurrent load.

## 2. Target Metrics
| Metric | Target | Rationale |
| --- | --- | --- |
| **P95 Latency (Read Operations)** | < 150 ms | Fast UI responsiveness in React & Flutter |
| **P95 Latency (Write Operations)** | < 250 ms | Strict transactional integrity |
| **Error Rate under Load** | < 0.1% | High system reliability |
| **Throughput Target** | > 100 req/sec | Peak gym check-in and booking periods |

## 3. k6 Test Script Plan
A k6 test script in `tests/performance/load-test.js` exercises:
- `/health` diagnostic ping
- `/api/system/info` metadata fetch
- `/api/classes` timetable lookup
- `/api/inventory` stock checks
