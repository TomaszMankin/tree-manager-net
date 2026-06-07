# ADR-023 — Root-person selection for 'Generuj' commands

**Status**: Accepted
**Date**: 2026-06-07

## Context

The 'Generuj Drzewo' and 'Generuj Rody' commands require a nominated root person whose
identifier is persisted per tree root. The persistence contract (`IFolderTreeSettingsStore`)
has always provided both a getter and a setter. However, the setter was never called from
the UI layer after an earlier refactoring removed the person-picker that was previously
embedded directly in those commands.

As a result, 'Generuj' commands always find the root-person identifier absent and
display an error message without performing any work, regardless of how many people
are in the tree.

## Decision

A dedicated 'Wybierz osobę główną' command is added to the application's main command
surface. The command uses the existing person-picker and directory-service collaborators
(already available via the edit-dependencies record) to let the user select a person,
then writes their identifier via `IFolderTreeSettingsStore.SetRootPersonId`. The
selection is confirmed to the user via the status bar.

The `IFolderTreeSettingsStore` contract and its infrastructure implementation are
unchanged. No new interfaces or records are introduced; the command reuses the
already-injected collaborators.

## Alternatives considered

**Embedding picker back inside each Generuj command**: Rejected. Selection of the root
person is a one-time setup step, not a per-generation step. Embedding it couples a
configuration concern to a generation command and makes both harder to test.

**New `SetRootPersonDependencies` record**: Rejected. The directory service and person
picker are already injected via the existing edit-dependencies record. Duplicating
them in a new record adds indirection without isolation benefit.

## Consequences

- 'Generuj Drzewo' and 'Generuj Rody' work for the first time after a clean install.
- The root person survives across sessions (written to `<root>/.PyTreeManager/settings.json`).
- Changing the root person requires invoking the new command explicitly; no migration
  of existing settings.json files is needed.
