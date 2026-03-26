# Contributing to Courier

Thank you for your interest in contributing!

## Getting started

1. Fork and clone the repository
2. Install [.NET 8 SDK](https://dotnet.microsoft.com/download)
3. Run `dotnet build` and `dotnet test` to verify everything works

## Workflow

- **Bug reports** — open an issue with a minimal reproduction
- **Feature requests** — open an issue for discussion before writing code
- **Pull requests** — target the `main` branch; keep PRs focused on a single concern

## Code style

- Follow existing naming and formatting conventions
- Use `record` types for commands, queries, and events
- Keep public APIs documented with XML comments

## Tests

All changes must be covered by tests. Run the suite with:

```bash
dotnet test
```

## Commit messages

Use conventional commit format:

```
feat: add retry pipeline step
fix: unwrap TargetInvocationException from reflection
docs: add pipeline step example to README
```
