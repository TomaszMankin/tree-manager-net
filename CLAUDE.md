# tree-manager-net — Claude project config

## Stack

- .NET 10 WPF + CommunityToolkit.Mvvm 8.x (MVVM bindings/commands)
- Serilog + Serilog.Sinks.File (structured logging)
- xUnit 2.x + NSubstitute (tests)
- Microsoft.Extensions.DependencyInjection (DI container, wired in App)
- Windows-only: `net10.0-windows` on App + App.Tests; `net10.0` on Core + Infrastructure

## Solution layout

```
TreeManager.sln
src/
  TreeManager.Core/           net10.0 — domain entities, interfaces, validators
  TreeManager.Infrastructure/ net10.0 — implementations of Core interfaces
  TreeManager.App/            net10.0-windows — WPF views, ViewModels, DI wiring
tests/
  TreeManager.Core.Tests/           net10.0
  TreeManager.Infrastructure.Tests/ net10.0
  TreeManager.App.Tests/            net10.0-windows
docs/
  decisions/    ADRs (ADR-001 through ADR-013 planned)
  domain.md     domain glossary — created in issue #19
```

## Onion rule

ALL interfaces (`IXxx`) live in `src/TreeManager.Core/Abstractions/<area>/`.
Implementations in `src/TreeManager.Infrastructure/<area>/`.
Core references NOTHING external (no Infrastructure, no App, no third-party SDKs except CommunityToolkit.Mvvm if/when domain base classes need it).
Infrastructure may reference Core.
App may reference both Core and Infrastructure.
Enforced structurally: Core.csproj has zero `<ProjectReference>` elements.

## Commit convention

Conventional Commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.
Scope optional but useful: `feat(core): add MeFile record`.

## ADR format

4 sections — **Problem | Decision | Rejected alternatives | Consequences**. Max 1 page. Caveman-compressed: drop articles/filler, keep every technical word intact. Location: `docs/decisions/ADR-NNN-kebab-title.md`. Index: `docs/decisions/INDEX.md`. Template: `docs/decisions/ADR-000-template.md`.

## Domain glossary

`docs/domain.md` — all domain concepts, Polish terms, algorithms, invariants. Created in sprint-19 (issue #19). Until then, defer domain questions to the GitHub issue body for the sprint touching that term.

## Parity with py-tree-manager

This repo is the .NET 10 WPF successor to `TomaszMankin/py-tree-manager`. Same domain: family-tree `me.json` files, Polish-language UI, single elderly Windows user. py-tree-manager stays in maintenance mode (bugfixes only) until WPF parity reached (issues #1–#20 closed). Bugs found in Python: fix in Python AND add as acceptance criteria for the matching WPF ticket.

## Test tiers

- **L0**: pure unit, zero I/O, zero filesystem, zero network. All mocks. Fast.
- **L1**: real filesystem under tmp paths. No UI. No network.
- **L2**: headed WPF UI automation (sprint-20).
- **e2e**: full end-to-end (sprint-20+).

xUnit `[Trait("Tier", "L0")]` attribute gates CI filter: `dotnet test --filter "Tier=L0"`.

## CI

GitHub Actions, self-hosted Windows runner (`runs-on: [self-hosted, windows]`).
Workflow: `.github/workflows/ci.yml` — restore → build (Release) → L0 → L1 → upload results.
Composite action: `.github/actions/run-test-tier` — parameterised by `tier` input. Add new tiers by invoking with a new `tier:` value — no action changes needed.
L2/e2e steps present in workflow but gated `if: false` — flip to `if: success()` when suites exist (sprint-20).

## Known build quirks

`TreatWarningsAsErrors=true` set globally in `Directory.Build.props`. WPF-generated `.g.cs` files may surface nullable warnings; if encountered suppress specific warning IDs in `TreeManager.App.csproj` via `<NoWarn>` — do NOT relax the global setting.
