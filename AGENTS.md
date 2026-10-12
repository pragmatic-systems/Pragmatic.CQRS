# AGENTS.md

Lightweight CQRS mediator library for .NET (net8.0 + net10.0) with MediatR-compatible interfaces.

## Layout

- `src/Pragmatic.CQRS` — the library (Avoid additional dependencies)
- `tests/Pragmatic.CQRS.Tests` — unit tests (Microsoft Testing Platform, not VSTest)
- `tests/Pragmatic.CQRS.Benchmark` — benchmarks
- `Pragmatic.CQRS.slnx` — solution file (slnx format)

## Commands

```bash
dotnet cake --Target=BuildAndTest       # build + test
dotnet cake --Target=BuildAndBenchmark  # build + benchmark
```

## Conventions

- Keep the public API MediatR-compatible (`IRequest`, `IRequestHandler`, `INotification`, `INotificationHandler`, `IPipelineBehavior`); don't add dependencies beyond the two abstractions.
- Style is enforced by `.editorconfig` — follow it (file-scoped namespaces, expression-bodied members, CRLF, UTF-8 BOM, usings outside namespace).
- Husky git hooks run on restore (`dotnet tool restore`); don't work around them.
- Target frameworks are `net8.0` + `net10.0` (library and tests); benchmarks are pinned to `net10.0`; SDK 10 via `global.json`.
