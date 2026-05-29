# ADR-007 — IPersonDirectoryService: dedicated Core abstraction for tree listing

**Status:** Accepted
**Date:** 2026-05-28

## Problem

Family-tab pickers need a filtered person list; edit-tree (issue #9) and validator (issue #14) need the same. Inline composition of the lower-level scan + read + project chain per caller duplicates projection logic and leaks Infrastructure shape into App. Sets precedent for future directory-level services.

## Decision

Dedicated interface in Core/Abstractions/Persistence encapsulates listing + summary projection — callers see one Core type, not three composed steps. Returns fully-materialised list synchronously; family-tree domain max ~1000 people, async adds cost without benefit. Root path per-call (not ctor-injected) keeps service stateless across issue #9 root-switching. Projection lives in Infrastructure. Future rule: new directory-level service justified when projection is non-trivial AND has 2+ callers — e.g. listing people for a picker (non-trivial projection, 3 callers) warrants a service; reading one file by path (trivial, single caller) stays inline composition.

## Rejected

- **Inline composition per caller** — projection duplicated across 3+ future callers; Infrastructure shape leaks into App.
- **Async streaming** — no benefit at single-family scale; revisit only if community-tree (10k+) scope added.
- **Ctor-inject root** — stateful coupling breaks on issue #9 root-switch.

## Consequences

- Good: one change point for projection; stateless across root-switching; clear pattern for issue #9/#14 services.
- Bad: full list materialised per call, no caching; sync API needs breaking change if scale ever grows.
