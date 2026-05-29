# ADR-006: Family tab cross-exclusion is per-session, not enforced at filesystem layer

Status: Accepted
Date: 2026-05-28

## Problem

The Family tab presents four relationship pickers (Parents, Children, Spouses, Siblings). A person may not hold multiple roles simultaneously — selecting someone as a parent should prevent selecting them as a spouse or sibling. The question is where this constraint is enforced: at the ViewModel boundary (per-session, UI-only) or at the filesystem layer (persistent, written into me.json).

## Decision

- Cross-exclusion enforced in ViewModel layer only, scoped to the person currently being edited; filesystem layer accepts whatever the ViewModel produces.
- Cross-exclusion lives in the ViewModel that aggregates the four pickers, recomputed reactively on any selection change across all four.
- The loaded person is excluded from every picker's candidate list (self-exclusion).
- No cross-role constraint is stored in or validated against the persisted file — the filesystem layer is write-only at save time.

## Rejected alternatives

- **Filesystem enforcement (validate on read/write)**: Would require reading all four relationship lists and cross-checking IDs on every save — high cost for a constraint that real data may already violate in legacy files. Deferred to a future consistency validator (issue #14).
- **Single unified relationship list**: Collapses role semantics into a flag field; harder to query and render by role type.

## Consequences

+ Exclusion logic is fast and stateless — no I/O on every selection change.
+ Existing me.json files with overlapping roles (legacy data) load without error; the constraint applies only to new edits on the loaded person.
- A person saved in two roles across separate edits is not prevented; issue #14 (`TreeConsistencyValidator`) is the intended resolution.
- State is per-person, not per-app-session — must reset fully when loading a different person (issue #9): all selections cleared, loaded-person id updated. Exclusions from person A must not carry into person B's editing. B set as A's child disappears from A's dropdowns, but reappears for C if not set in any of C's roles.
