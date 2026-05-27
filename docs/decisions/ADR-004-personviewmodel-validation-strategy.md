# ADR-004 — PersonViewModel field defaults and "(nieznane)" coercion

**Status:** Accepted
**Date:** 2026-05-25

---

## Problem

Tab 1 (Dane osoby) must handle partial person records. Name fields (FirstName, LastName) and Sex are all genuinely optional — a person in a family tree may be completely unknown (e.g. a referenced sibling from the 1700s). Forcing validation errors on empty/unknown values would block saving legitimate records.

---

## Decision

- `PersonViewModel` inherits `ObservableObject` — no validation framework needed.
- No `[Required]` or `[CustomValidation]` on any field. All fields are optional.
- `Sex` defaults to `Sex.Unknown` — a valid state meaning "sex not known". All three enum values (Unknown, Male, Female) are valid on the VM.
- Empty `FirstName` / `LastName` are valid on the VM. `PersonViewModelMapper.ToMeFile()` coerces empty strings to `"(nieznane)"` at persist time, keeping the JSON consistent with py-tree-manager convention.
- Mapper split by extended type: `MeFileMapper` (`ToViewModel(this MeFile)`) and `PersonViewModelMapper` (`ToMeFile(this PersonViewModel)`).

---

## Rejected

- **`ObservableValidator` + `[CustomValidation]` on Sex** — `Sex.Unknown` is a legitimate value; treating it as a validation failure blocked valid records.
- **`[Required]` on FirstName / LastName** — same problem; unknown persons have no name, not an error.
- **Coercing `"(nieznane)"` in VM setter** — VM should hold the user-visible value (empty string); coercion belongs at the persistence boundary.
- **Single `MeFileMapper` class for both directions** — extension classes named by the `this` type are clearer; each class has one direction of mapping.

---

## Consequences

+ PersonViewModel is a simple data bag — no annotation overhead, easy to construct in tests.
+ `"(nieznane)"` sentinel written by mapper means JSON files stay consistent with py-tree-manager output.
