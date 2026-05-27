# ADR-001: Stack and project layout

Status: Accepted
Date: 2026-05-24

## Problem
py-tree-manager (Python 3.11 + wxPython 4.2.4) hits scaling wall: 1600-line file, wheel install fragility, no headless testing, loose typing. Need rewrite to typed stack on Windows with clear layer rules from day 0.

## Decision
.NET 10 WPF. Onion: `TreeManager.Core` (net10.0) holds domain entities + all interfaces under `Abstractions/<area>/`. `TreeManager.Infrastructure` (net10.0) holds Core-interface implementations. `TreeManager.App` (net10.0-windows) holds WPF views + ViewModels + DI wiring. Dependencies point inward only — App references Core + Infrastructure; Infrastructure references Core; Core references nothing external. Stack: CommunityToolkit.Mvvm (MVVM bindings/commands), Serilog + Serilog.Sinks.File (structured logging), xUnit + Moq (tests). `Directory.Build.props` enforces `Nullable=disable`, `TreatWarningsAsErrors=true`, `LangVersion=latest` globally.

## Rejected alternatives
- PySide6: same Python ecosystem fragility; two stacks remain in parallel
- .NET MAUI: cross-platform overhead, no benefit (Windows-only user), less mature desktop forms
- Stay wxPython: already at scaling wall, no path to headless testing
- WinForms: weaker data-binding ergonomics, no MVVM story without extra plumbing
- .NET 8: .NET 10 already installed locally; longer LTS window; no downgrade benefit

## Consequences
+ Strict Onion enforced structurally via csproj `<ProjectReference>` — Core cannot accidentally depend on Infrastructure
+ All interfaces findable at `Core/Abstractions/<area>/IXxx` — consistent discoverability
+ Test tiering (L0/L1/L2/e2e) via xUnit Trait + composite CI action ready from sprint-01
- `net10.0-windows` hard-dependency on App project — build fails on non-Windows
- Migration cost ~20 sprints to reach feature parity with py-tree-manager
