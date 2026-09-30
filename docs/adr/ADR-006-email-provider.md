# ADR-006: Third-Party Email Provider & Resilient Integration

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: Integration & Backend Team  

---

## 1. Context & Problem Statement
When high-cost equipment repair orders are authorized by a manager, the system must dispatch a Request for Quotation (RFQ) or service order email to certified equipment vendors (e.g. LifeFitness Certified Logistics). The integration must:
- Prevent duplicate emails upon retry or user double-click (Idempotency).
- Prevent unapproved RFQ dispatches (Security gate).
- Survive third-party network outages without rolling back the persisted database repair order (Degraded fault tolerance).

---

## 2. Options Considered
1. **Resilient Transactional Email Adapter with Degraded Mode & Idempotency**:
   - Encapsulated in `SendVendorEmailTool` with unique idempotency keys (`vendor-action-{issue_id}-{action_type}`).
   - Verifies `approval_status == 'APPROVED'`.
   - On network failure, logs the incident, falls back to spooling/logging, and records a warning without crashing the workflow.
2. **Synchronous Direct SMTP Client in API Controller**:
   - Calling standard `System.Net.Mail.SmtpClient` directly within the HTTP request.
3. **External Queue (RabbitMQ / AWS SQS)**:
   - Offloading email dispatch to a separate message queue worker service.

---

## 3. Decision
We chose **Option 1: Resilient Transactional Email Adapter (`SendVendorEmailTool`)**.

---

## 4. Consequences
### Positive Consequences:
- **Zero Financial Discrepancies**: RFQ cannot be sent unless approved by management.
- **Idempotency**: Duplicate requests return cached message IDs (`MSG-XXXX`).
- **High Availability**: Third-party outages do not break the core gym operations or rollback database records.

### Negative Consequences:
- Degraded mode requires manual operational monitoring of the spooler queue.

---

## 5. Rejected Alternatives
- **Direct Controller SMTP**: Rejected because third-party email latency (1-3 seconds) blocks HTTP threads, and failure crashes user transactions.
- **RabbitMQ / SQS**: Rejected as unnecessary infrastructure overhead for an academic assignment baseline.
