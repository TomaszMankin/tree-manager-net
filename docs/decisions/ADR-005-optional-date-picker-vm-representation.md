# ADR-005 — OptionalDatePickerViewModel: int? with null for unknown

**Status:** Accepted
**Date:** 2026-05-27

---

## Problem

PartialDate wire format uses "XX" sentinel strings for unknown day/month/year. The ViewModel layer needs a representation for user interaction — store the sentinel string or use typed nullability?

---

## Decision

- `OptionalDatePickerViewModel` stores `int? Day`, `int? Month`, `int? Year` — null means unknown.
- "XX" sentinel exists only in serialized wire format.
- VM never holds "XX" strings — conversion at the mapper boundary only.
- Day options list recalculates when Month changes; selected day resets to null if it exceeds the new month's maximum.
- `IsDeceased` derived from `DatesOfDeath` being non-empty — no flag added to `MeFile`.
- `DeathDate.IsEnabled` defaults to false (mirrors `IsDeceased` default of false); enabled only when `IsDeceased` is set true.

---

## Rejected

- **string? with "XX" in VM** — leaks wire format into presentation layer; complicates binding and ComboBox item matching.
- **Explicit `IsDeceased` flag in MeFile** — py-tree-manager schema has no such field; derive from DatesOfDeath presence instead.

---

## Consequences

+ VM is cleanly typed — ComboBox binds int? directly, no string parsing in view.
+ `"XX"` coercion isolated to `PartialDateExtensions.ToSerializedString()` / `ToPartialDate()`.
