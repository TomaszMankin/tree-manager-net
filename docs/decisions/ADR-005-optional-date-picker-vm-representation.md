# ADR-005 — OptionalDatePickerViewModel: string fields with validation

**Status:** Accepted
**Date:** 2026-05-27 (revised 2026-05-28)

---

## Problem

### Why partial dates are required

Family tree archaeology deals with sources of varying completeness. For any given person, date knowledge may be:

- **Complete** — "born 12 March 1947" (full records, living relatives)
- **Year only** — "born 1847" (parish register lists year, no day/month)
- **Partial year** — "born in the 1840s" → year stored as "184-"
- **Day + month, no year** — name-day records, no birth year established
- **Unknown entirely** — person confirmed to exist from other records but no date found
- Any other combination of the above

This is not an edge case — it is the normal state for most entries beyond two or three generations back. The data model must represent each date component independently as known or unknown, and years must be storable as partial strings (e.g., "184-", "18--") not just as complete integers.

### How to represent this in the ViewModel

The ViewModel layer needs a representation for user interaction that supports the full range of partial-date knowledge above.

---

## Decision

- All three date components (day, month, year) are independently optional and entered as free text.
- Year allows partial values not expressible as integers: any 4-char string where each char is a digit or `-`, no ordering constraint — middle wildcards valid. Examples: `"1947"`, `"194-"`, `"19-4"`, `"18--"`, `"1---"`, `"----"`.
- Invalid input is shown visually; the user is not blocked from saving work-in-progress.
- `IsDeceased` derived from `DatesOfDeath` being non-empty — no flag added to `MeFile`.
- `DeathDate` enabled only when `IsDeceased` is true.

---

## Rejected

- **Dropdowns with integer fields** — cannot represent partial years ("184-"); forces binary known/unknown on year; no path to expressing decade-level knowledge.
- **Explicit `IsDeceased` flag in `MeFile`** — `MeFile` schema has no such field; derive from `DatesOfDeath` presence instead.

---

## Consequences

+ Each date component independently unknown — correct model for historical records.
+ Partial years ("184-", "19-4", "18--") expressible and preserved through the full data round-trip.
+ Validation is advisory, not blocking — preserves ability to save work-in-progress.
- Free-text removes automatic day-range clamping; user sees validation feedback instead of prevention.
