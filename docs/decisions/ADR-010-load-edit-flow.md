# ADR-010 — Load and edit flow

**Status:** Accepted
**Date:** 2026-05-30

---

## Problem

Loading an existing person for editing requires: reading their file, populating all three tab sub-models, capturing a snapshot for delta-diff on save, and routing Save to Update vs Create. Several design choices needed resolution.

---

## Decision

- Person loader service lives in App layer — it operates on App-layer view-model types; Core Onion cannot reference App types.
- Edit-related dependencies grouped into a single record — keeps the main view-model constructor below the D-003 seven-parameter threshold; mirrors the existing pattern for root-picker grouping.
- Original snapshot passed into Update by caller — repository stays stateless; snapshot semantics belong to the call site. Enables L0 testing without filesystem setup.
- Relationship delta diff lives entirely in the persistence Update entry point — single change-logic location; consistent with ADR-009 rule.
- Save routing decided by snapshot presence: absent → create new record; present → update existing. Snapshot refreshed after successful update so subsequent saves diff from latest persisted state.
- Switching back to add mode clears the snapshot — prevents stale routing to update when user re-enters add flow after editing.
- Person picker wrapped in an interface — enables L0 testing of open-person flow without WPF; mirrors existing root-picker abstraction.
- Root path guard on both save and open-person paths — logs warning, returns early, busy flag reset in finally.
- Error message observable on main view-model — set on any exception in save or open; cleared on success; surfaced to UI via binding.

---

## Rejected

- **`IPersonLoaderService` in Core** — Core cannot reference App-layer ViewModels.
- **Snapshot captured inside repository** — repository would be stateful across calls; breaks testability and ADR-009 "no state" principle.
- **Single `Save(person, snapshot)` with null-sentinel** — collapses distinct Create/Update semantics; intent obscured.
- **Direct `PersonPickerDialog` construction in `OpenPersonCommand`** — blocks L0 testing; same reason `IRootPickerService` was introduced.
- **`IServiceProvider` injection in `MainViewModel`** — hides dependencies; makes test setup opaque.

---

## Consequences

+ `OpenPersonCommand` fully testable at L0 via mocked `IPersonPickerService`.
+ Delta sync produces zero writes when no relationships changed.
+ `MainViewModel` ctor stays at 6 explicit params; future 7th param triggers grouped-param refactor.
- Name-based path resolution (`DisplayName` = folder name) is brittle if folder and `person_name` diverge; issue #14 is the intended validator.
- Folder rename when name fields change deferred to issue #11.
