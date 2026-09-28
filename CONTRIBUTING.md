# Contributing to SmartGym

Thank you for contributing to SmartGym! This project is an enterprise-style, polyglot software engineering frameworks implementation developed for **SE3090 Assignment 1**.

## Development Principles
1. **Authoritative Backend**: All business logic and validations must reside in the ASP.NET Core Web API. Frontends (React and Flutter) must never query databases or trigger internal AI/email endpoints directly.
2. **Phase-by-Phase Execution**: Implement only the designated phase. Do not jump ahead to future phases.
3. **No Mock Data for Core Business Features**: Real relational models in PostgreSQL and verifiable domain logic must back all operations.
4. **No Hardcoded Secrets**: Use environment variables or `.env` configurations. Never commit API keys or production connection strings.
5. **Academic Integrity**: Never fabricate test runs, Git commits, or deployment claims.

## Local Development Setup
See [README.md](file:///c:/Users/M%20S%20I/Downloads/SEF%20p/README.md) for full setup instructions.
