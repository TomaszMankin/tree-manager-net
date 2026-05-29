# ADR-009 — Save orchestration

**Status:** Accepted
**Date:** 2026-05-30

---

## Problem

Saving a new person requires: assembling a complete data record from multiple tab sub-models, writing a folder and file on disk, and updating every related person's file with the reverse relationship. The question is which layer owns which part and how they are wired together.

---

## Decision

- ViewModel layer owns assembly: combines sub-model data into a single record before any I/O. No I/O in ViewModel.
- Dedicated persistence abstraction owns folder creation, file writing, and bidirectional sync. Callers see one entry point with no coordination logic.
- Bidirectional sync is a save concern, executed inside the persistence layer after the person's own file is written. Failure on one related file is logged and skipped — does not abort remaining writes.
- Bidirectional sync rule: mirror the direct pair only; no transitive inference; no overwrite on type mismatch; idempotent via UUID presence check. (Confirmed by issue #8 user feedback.)
- Sync finds related persons' files by scanning the tree root once at save time and building a UUID-to-path index. No caching between saves.

---

## Rejected

- **ViewModel doing file I/O inline** — untestable at L0; ViewModel would need filesystem dependencies.
- **App-layer save orchestrator service** — single caller, pure delegation; thin pass-through with no logic of its own violates the 2+ caller rule from ADR-007.
- **Core-layer save service** — Core cannot reference Infrastructure; no I/O allowed in Core.
- **IFileTransaction rollback** — create-new path has no pre-existing state; async rollback API does not match synchronous processor API; deferred to issue #9 when edit-existing use-cases drive the shape.

---

## Consequences

+ ViewModel tests need no filesystem mocks; repository tests need no ViewModel.
+ One save entry point; callers are unaffected by future persistence changes.
- O(n) filesystem reads per save (scan all me.json to build index); acceptable at single-family scale (~500 people).
- No rollback if person file writes but related file writes partially fail; issue #9 to revisit.
