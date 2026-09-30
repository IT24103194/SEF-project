# ADR-002: Flutter State Management Architecture

**Status**: Accepted  
**Date**: September 2026  
**Deciders**: Mobile & Architecture Team  
**Consulted**: Senior Flutter Engineer  

---

## 1. Context & Problem Statement
The SmartGym Mobile Application (`smartgym_mobile`) serves both gym members and personal trainers. It manages:
- Secure JWT persistence and reactive authentication status.
- Real-time facility issue submissions with camera photo attachments.
- Class schedule browsing with dynamic remaining capacity updates.
- Trainer attendance rosters with atomic check-in state.

We require a compile-time safe, declarative state management library that supports reactive UI rebuilds, seamless mocking in widget tests, and clean separation between UI widgets and network clients.

---

## 2. Options Considered
1. **Flutter Riverpod (v2.5+)**: Compile-safe, global-variable-free provider architecture with `StateNotifier` and `AsyncNotifier`.
2. **BLoC (Business Logic Component)**: Reactive stream-based architecture using `flutter_bloc` and events.
3. **Provider**: Legacy inherited widget wrapper.
4. **GetX**: High-level micro-framework combining navigation, state, and DI.

---

## 3. Decision
We chose **Option 1: Flutter Riverpod (`flutter_riverpod: ^2.5.1`)**.
We structured state around declarative providers:
- `authNotifierProvider`: Manages session tokens, secure storage sync, and auto-login.
- `facilityIssuesProvider`: Handles facility ticket streams and status refreshes.
- `classesNotifierProvider`: Manages class schedules and remaining spots.
- `attendanceNotifierProvider`: Handles trainer attendance check-in operations.

---

## 4. Consequences
### Positive Consequences:
- **Compile-Time Safety**: No `ProviderNotFoundException` runtime crashes.
- **Dependency Injection**: Services and API clients are injected via provider references (`ref.read`, `ref.watch`).
- **Testability**: Providers can be overridden cleanly in unit and widget tests using `ProviderScope(overrides: [...])` without complex mock boilerplate.
- **Clean Architecture**: Separates UI presentation from business logic and network clients.

### Negative Consequences:
- Learning curve associated with `ConsumerWidget`, `WidgetRef`, and `StateNotifier`.

---

## 5. Rejected Alternatives
- **BLoC**: Rejected due to high event/state boilerplate for simple CRUD operations like attendance marking and feedback submission.
- **Provider**: Rejected due to runtime scoping errors and lack of modern async notifier capabilities.
- **GetX**: Rejected because it relies on static global contexts that degrade unit and widget testability.
