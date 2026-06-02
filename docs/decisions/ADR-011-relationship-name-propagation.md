# ADR-011 — Relationship display-name propagation on update

**Status:** Accepted
**Date:** 2026-05-31

---

## Problem

Relationship lists hold UUID plus a cached display name per related person. When a person's name changes on update, the cached copies held by every related person go stale. Must decide whether to keep cached names at all, and whether to refresh them when a name changes.

---

## Decision

- Keep cached display names alongside UUIDs. UUID is authoritative; name is a read-time convenience copy. Avoids a tree-wide scan on every person open and preserves wire-format interop with the sibling Python app.
- On update, when the saved person's name differs from its pre-edit snapshot, refresh the cached name in every related person's matching relationship entry. Names stay honest without manual re-save.
- Propagation reuses the same related-person index already built for bidirectional relationship sync — no extra scan, write count proportional to number of related persons.
- Propagation is a save-side concern, executed in the persistence layer after the person's own file is written, alongside existing relationship sync. One related-file failure is logged and skipped — does not abort remaining writes.
- Folder rename on name change stays out of scope; tracked under issue #11. Cached-name refresh does not depend on it.

---

## Rejected

- **Accept stale names** — leaves cached copies wrong until each related person is manually re-saved; sibling Python app refreshes its references on name change, so this would diverge from interop behaviour, not match it.
- **UUID-only relationship lists** — drops the cached name entirely; breaking wire-format change versus the sibling Python app, which reads these fields; forces a tree-wide lookup on every open.
- **Refresh on read instead of on write** — pushes a directory scan into every load; cached copy on disk would still rot for any external reader.

---

## Consequences

+ Cached names current across the tree after any name-changing save; no manual fan-out re-save.
+ Reuses existing related-person index; no second filesystem scan.
+ Wire format unchanged; sibling Python app keeps reading the same fields.
- One extra targeted write per related person on a name-changing save; negligible at single-family scale.
- Partial-failure window unchanged from existing sync: a related file that fails to write keeps its old cached name until the next save touches it; logged.
- Cached name and on-disk folder name can still diverge until folder rename lands (issue #11); name resolution by folder remains brittle until then.
