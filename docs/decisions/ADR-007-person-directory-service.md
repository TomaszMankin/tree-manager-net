# ADR-007 — IPersonDirectoryService: dedicated Core abstraction for tree listing

**Status:** Accepted
**Date:** 2026-05-28

## Problem

Sprint-07 pickers need a filtered person list; sprint-09 (edit-tree) and sprint-14 (validator) need the same. Inline composition of the lower-level scan + read + project chain per caller duplicates projection logic and leaks Infrastructure shape into App. Sets precedent for future directory-level services.

## Decision

Dedicated interface in Core/Abstractions/Persistence encapsulates listing + summary projection — callers see one Core type, not three composed steps. Returns fully-materialised list synchronously; family-tree domain max ~1000 people, async adds cost without benefit. Root path per-call (not ctor-injected) keeps service stateless across sprint-09 root-switching. Projection lives in Infrastructure. Future rule: new directory-level service justified when projection is non-trivial and has 2+ callers.

## Rejected

- **Inline composition per caller** — projection duplicated across 3+ future callers; Infrastructure shape leaks into App.
- **Async streaming** — no benefit at single-family scale; revisit only if community-tree (10k+) scope added.
- **Ctor-inject root** — stateful coupling breaks on sprint-09 root-switch.

## Consequences

- Good: one change point for projection; stateless across root-switching; clear pattern for sprint-09/14 services.
- Bad: full list materialised per call, no caching; sync API needs breaking change if scale ever grows.
