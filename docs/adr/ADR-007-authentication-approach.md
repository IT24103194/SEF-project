# ADR-007: Authentication & Token Authorization Strategy

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: Security & Architecture Team  

---

## 1. Context & Problem Statement
SmartGym requires secure authentication across heterogeneous clients:
- Web Admin Console (React SPA)
- Mobile Client (Flutter on Android/iOS)
- Background AI service communication

The mechanism must protect against credential theft, session hijacking, and privilege escalation, while enforcing strict Role-Based Access Control (RBAC) across `Admin`, `Manager`, `Trainer`, and `Member`.

---

## 2. Options Considered
1. **Dual-Token JWT (Short-Lived Access Token + Revocable Database Refresh Token)**:
   - Access token: 15-minute expiry, stateless HMAC-SHA256 signature, carrying role claims.
   - Refresh token: 7-day expiry, cryptographically random, stored in PostgreSQL with revocation timestamps.
2. **Stateful Server-Side Cookie Sessions**:
   - Storing session IDs in Redis / Database on every request.
3. **OAuth2 / OpenID Connect Identity Provider (Auth0 / IdentityServer)**:
   - Delegating authentication to an external third-party cloud IDP.

---

## 3. Decision
We chose **Option 1: Dual-Token JWT with Revocable Refresh Tokens**.
- Handled centrally by `AuthService` in `SmartGym.Api`.
- Enforced on controllers via `[Authorize(Roles = "...")]`.
- Client storage:
  - Flutter: `FlutterSecureStorage` (AES-256 hardware keychain).
  - React: Memory state + Axios interceptors for automatic token refresh.

---

## 4. Consequences
### Positive Consequences:
- **Stateless Verification**: High API performance without hitting the database for every single read request.
- **Immediate Revocation**: Malicious or compromised tokens can be invalidated via `/api/auth/revoke-token`.
- **Role Isolation**: RBAC claims are verified cryptographically in memory by ASP.NET Core middleware.

### Negative Consequences:
- Access tokens cannot be revoked until their 15-minute lifespan expires, mitigated by short expiration.

---

## 5. Rejected Alternatives
- **Stateful Cookie Sessions**: Rejected because mobile clients (Flutter) handle Bearer authorization headers more reliably than browser cookie jars.
- **External Identity Provider (Auth0 / IdentityServer)**: Rejected to ensure 100% offline, self-contained evaluation during university defense without third-party internet dependencies.
