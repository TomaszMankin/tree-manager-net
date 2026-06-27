# L3 UI automation + folder-graph verification — design

Date: 2026-06-27
Status: approved (brainstorm), pending implementation plan
Branch/PR: feat/sprint-22-cutover-ux-fixes / PR #50
Tracking decision: D-021

## Problem

The app is WPF. L0/L1 tests cover logic and the on-disk repository but never launch
the real window. Several user-visible bugs recurred across sprints 25–27 — false
"unsaved changes" prompts, multiline Enter behaviour, the relationship-folder /
shortcut graph — because automated tests passed against mocked behaviour while the
actual UI was broken. There is no automated test that drives the real application and
then proves the resulting on-disk folder/file/shortcut graph is correct, especially at
scale (e.g. a tree with many people).

This design adds that missing tier (L3): drive the real UI, then verify the physical
graph on disk.

## Goals

- Drive the actual WPF window through real user gestures (FlaUI / UI Automation).
- After a scenario runs, verify the complete on-disk graph: folders, `me.json`
  relationship arrays, and the bidirectional `.lnk` shortcut mirror.
- Catch both *missing* artifacts (creation bugs) and *leftover* artifacts
  (modification/deletion bugs).
- Keep scenarios simple to author and refactor-safe.
- Run on an isolated host so the suite never takes over the developer's machine, with
  disposable per-test data.

## Non-goals

- Replacing L0/L1. L3 is the top of the pyramid: few, high-value end-to-end scenarios.
- Running on GitHub-hosted runners or the existing `NETWORK SERVICE` runner (no
  interactive desktop). L3 runs only on the dedicated interactive runner.
- A standalone data seeder. The scenario itself creates the data through the UI.
- Containerising the GUI run (WPF needs a real desktop; not containerisable).

## Decisions (locked in brainstorming)

1. **Scenario description = ordered .NET fluent DSL.** Not JSON/YAML — an interpreter
   is an error-prone layer that drifts from the app and isn't compiler-checked. The
   DSL is C#, type-checked, debuggable, refactor-safe. Steps run in explicit order
   (deterministic regression, not random/monkey testing).
2. **Single source of expectation = the DSL call (option 2a).** Each fluent call both
   performs the UI gesture and appends to an in-memory expected graph. No intermediate
   serialized file (no serialize/deserialize error layer). The expected model is the
   accumulated state of the DSL calls.
3. **Verification = whole-graph compare at the end, set-equality both directions.**
   Not incremental per-action assertions (which force tracking each action's blast
   radius across other folders). One disk walk builds the actual graph; assert
   `actual == expected`. Both directions: *missing* (expected − actual) catches
   creation bugs; *extra* (actual − expected) catches stale/orphan artifacts from
   modifications. Shortcuts compare by **resolved target**, not mere existence — a
   `.lnk` pointing at the wrong folder is a real bug.

## Architecture

Five components, each independently testable.

### 1. Shortcut resolve primitive
`IShortcutCreator` currently only creates `.lnk` files. Add a resolve operation that
reads a `.lnk` and returns its target path. The verifier needs this to build the
actual graph. Implemented in the existing shell shortcut creator via the same COM
interface that writes shortcuts. (Production code change; small, also independently
useful.)

### 2. AutomationIds on UI elements
FlaUI must target controls by stable identity, not by Polish display text (brittle:
text changes, layout changes, localisation). Add `AutomationProperties.AutomationId`
to the buttons, menu items, input fields, and dialogs the DSL drives. This is the only
production-XAML change and is inert at runtime.

### 3. Scenario DSL (L3 project)
A fluent C# API over FlaUI in a new `TreeManager.App.L3` test project. Launches the
app against a fresh temp root, exposes gestures (add person, set relationships, edit,
remove, save / save-as-draft / promote), and records each gesture into the expected
graph. Example shape:
```
scenario.AddPerson("Jan", "Kowalski")
        .WithSpouse("Anna", "Nowak")
        .WithChild("Olek", "Kowalski")
        .Save();
...
scenario.VerifyOnDisk();
```
Each gesture method: drives the UI via AutomationId, then mutates the expected graph.
Command bodies contain only UI driving; expectation recording lives in the DSL layer.

### 4. Expected graph model
Plain in-memory structure. Per-person node:
- identity (name fields → folder name),
- `me.json` relationship arrays (parents/children/spouses/siblings by reference),
- expected `.lnk` edges as `(subfolder, resolved-target-folder)` pairs.
Built up by DSL calls. No file, no serialization.

### 5. Disk-walk verifier
Reusable engine. Walks the temp root under `Lista osób/`; for each person folder reads
`me.json` and enumerates the four relationship subfolders
(`Rodzice`/`Dzieci`/`Małżonkowie`/`Rodzeństwo`), resolving every `.lnk` target.
Builds the actual graph, asserts set-equality against the expected graph in both
directions, links compared by resolved target. On failure, prints the diff (missing
set + extra set) so a failure names exact paths/links.

Default: called once at end of a scenario. Also callable mid-scenario for fail-fast
localisation while debugging — same engine, same code path.

## Data flow (two-phase)

```
fresh temp root  ──>  Launch app (FlaUI)  ──>  DSL gestures drive UI
                                                  │ (each gesture also records expected graph)
                                                  v
                              Close app  ──>  VerifyOnDisk(): walk root → actual graph
                                                  │
                                                  v
                              assert actual == expected (set-equality, both directions,
                              links by resolved target)  ──>  delete temp root
```

## Execution host

Dedicated Hyper-V VM (Windows 11) running a second self-hosted GitHub Actions runner
labelled `ui-tests`, in its own interactive desktop session (isolated from the
developer's session — the suite never grabs the host mouse/keyboard). Auto-logon lands
on the console session; the runner starts there via a Startup-folder shortcut (must NOT
be a Windows service — session 0 has no desktop). Per-test disposable temp roots give
data cleanliness; the VM checkpoint gives a machine-level clean baseline.

The VM provisioning is scripted in `Scripts/` (VM creation, test-session config,
tooling install, auto-logon, runner auto-start) with the full runbook in
`Scripts/README.md`. Known gotcha documented there: verify auto-logon in VMConnect
**Basic Session** — Enhanced Session is an RDP login into a separate session and hides
the auto-logon console.

## CI integration

A workflow (`.github/workflows/ui-tests.yml`) targeting `runs-on: [self-hosted,
ui-tests]`, triggered on a schedule (nightly) and `workflow_dispatch` (on demand) — not
on every push (slow; needs the live session). The existing runner keeps doing build +
L0/L1 on every push, untouched. L3 tests carry a distinct test trait/category excluded
from the default `dotnet test` run so local/`push` CI never tries to run them headless.

## Error handling

- Verifier failure → diff output (missing + extra sets, with resolved targets) naming
  exact paths/links.
- App launch / control-not-found → fail fast with a screenshot artifact.
- Per-test temp root always cleaned up (even on failure) via test teardown.

## Testing the test infrastructure

- The disk-walk verifier and expected-graph model are plain logic — unit-tested at L0
  against synthetic graphs (matching/missing/extra/wrong-target cases).
- The resolve primitive gets an L1 round-trip test (create a `.lnk`, resolve it, assert
  target) on real disk.
- The DSL itself is exercised by the L3 scenarios it powers.

## Open choices deferred to the implementation plan

- Exact FlaUI version and project scaffolding.
- Whether the expected model is built purely inline by the DSL, or the verifier
  re-derives the `me.json` half from disk and only the `.lnk` mirror is checked against
  arrays (a lighter 2b layer). Brainstorm chose 2a (full inline expectation); revisit
  only if duplication proves heavy.
- Scheduling cadence specifics (nightly time, retention of screenshots).
```
