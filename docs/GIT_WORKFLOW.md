# SmartGym Git Workflow & Branching Strategy

This branching strategy aligns directly with **Section 20 (Git, GitHub and CI/CD)** of the SmartGym Master Specification:

## Branching Hierarchy
```
main (Production / Stable Demonstration Release)
  ▲
develop (Integration Branch for Current Phase)
  ▲
feature/student1-inventory
feature/student2-facility-ai
feature/student3-class-booking
feature/student4-membership-goals
feature/react-admin-ui
feature/flutter-mobile-client
feature/ai-langgraph-service
```

## Commit Conventions
All commit messages must adhere to Conventional Commits:
- `feat(scope)`: A new feature
- `fix(scope)`: A bug fix
- `docs(scope)`: Documentation changes
- `test(scope)`: Adding or modifying test suites
- `refactor(scope)`: Code changes without modifying behavior
- `chore(scope)`: Build tools or dependency updates

### Example:
`feat(phase-1): project foundation, master specification integration, monorepo layout and git setup`

## Pull Request Rules
1. Never commit directly to `main`.
2. All feature branches must branch off `develop`.
3. CI checks (backend .NET 8 build/test, Python pytest, React build) must pass before merging.
4. Peer code review is required on all PRs to maintain academic evidence for the SE3090 viva examination.
