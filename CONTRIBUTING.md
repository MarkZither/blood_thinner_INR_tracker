# Contributing to Blood Thinner Tracker

Thank you for your interest in contributing! This is a medical application —
quality, safety, and security are paramount.

## Medical Application Guidelines

> **Important:** This application tracks blood thinner medication and INR results.
> All contributions that affect dosage calculations, medication scheduling, or
> INR range validation must be reviewed with extra care.

## Getting Started

1. Fork the repository at <https://github.com/MarkZither/blood_thinner_INR_tracker>
2. Set up your local environment — see [docs/DEVELOPMENT_SETUP.md](docs/DEVELOPMENT_SETUP.md)
3. Create a feature branch from `main`

```bash
git checkout -b feat/your-feature-name
```

## Development Requirements

- **.NET 10 SDK** — run `dotnet --version` to verify (must be `10.0.x`)
- **Docker Desktop** (for local PostgreSQL via .NET Aspire)
- OAuth credentials for local auth testing — see [docs/USER_SECRETS_SETUP.md](docs/USER_SECRETS_SETUP.md)

## Workflow

1. Check the [issue tracker](https://github.com/MarkZither/blood_thinner_INR_tracker/issues)
   for existing issues or open a new one before starting large work
2. Write or update tests — aim for **90% coverage** on business logic
3. Validate medical constraints: 12-hour medication window, INR ranges 0.5–8.0
4. Run the full test suite before submitting

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/) with a short summary line
(max 72 chars) and a concise body (max 8 lines total). See the commit guidelines in
[.github/copilot-instructions.md](.github/copilot-instructions.md#git-commit-guidelines).

## Pull Requests

- Fill in the PR template
- Link the related issue (e.g. `Closes #42`)
- Ensure CI passes before requesting review
- Security-sensitive changes (auth, encryption, medical data) require explicit sign-off

## Reporting Bugs

Use [GitHub Issues](https://github.com/MarkZither/blood_thinner_INR_tracker/issues).
For **security vulnerabilities**, follow [SECURITY.md](SECURITY.md) instead.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).
By participating you agree to uphold its standards.
