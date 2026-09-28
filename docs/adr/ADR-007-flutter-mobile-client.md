# ADR-007: Flutter 3 Cross-Platform Mobile Client Architecture

## Status
Accepted

## Context
Gym members and trainers require an intuitive mobile application to manage memberships, log fitness milestones, book classes, and report broken gym facilities with real-time status tracking.

## Decision
We chose **Flutter 3 with Dart**.

### Rationale:
- Single codebase targeting both Android and iOS devices.
- High-fidelity custom widgets and smooth 60fps animations.
- Native integration with camera and image picking for facility issue photo submissions.
- Secure storage of JWT tokens via standard platform keychain/keystore bindings.
- Communicates exclusively with the authoritative ASP.NET Core Web API backend.

## Consequences
- **Positive**: Consistent UI across platforms, offline-friendly token storage.
- **Negative**: Requires Dart build tools and mobile SDK setup for deployment packaging.
