# ADR-004 — PersonViewModel validation strategy

**Status:** Accepted
**Date:** 2026-05-25

---

## Problem

Tab 1 (Dane osoby) requires required-field validation on Imię, Nazwisko, Płeć with Polish error messages. Validation must trigger on edit (per-field, live) AND on explicit save-time bulk check. Two traps:
1. `Sex` enum — `Unknown = 0` is the default value. `[Required]` on an enum silently passes the default (0 is not null; `Required` allows it). So `[Required]` alone does NOT enforce "user must pick a sex".
2. CommunityToolkit.Mvvm generated setters only call `ValidateProperty` when a value is assigned — construction with defaults does NOT fire validation. A fresh VM has `HasErrors == false` even though required fields are empty.

---

## Decision

- `PersonViewModel` inherits `ObservableValidator` (CommunityToolkit.Mvvm).
- Each validated field carries both `[ObservableProperty]` and `[NotifyDataErrorInfo]` — the source generator emits setters that call `ValidateProperty` on every change.
- `FirstName` and `LastName` validated with `[Required(ErrorMessage = "...")]` (Polish text inline).
- `Sex` validated with `[CustomValidation(typeof(PersonViewModel), nameof(ValidateSex))]` — static method returns `ValidationResult("Płeć jest wymagana")` when `Sex == Sex.Unknown`.
- Public `ValidateAll()` delegates to `ValidateAllProperties()` — called explicitly at save time (sprint-08) to bulk-validate all fields regardless of whether setters fired.
- XAML bindings use `ValidatesOnNotifyDataErrors=True` for live visual feedback (WPF default red-border; no custom ErrorTemplate this sprint).

---

## Rejected

- **`[Required]` on `Sex`** — does not trip the enum default value 0 per `System.ComponentModel.DataAnnotations` spec. Would silently allow `Sex.Unknown` through as valid.
- **Custom `[SexRequiredAttribute : ValidationAttribute]`** — one usage site; 3-uses rule fails; `[CustomValidation]` is the idiomatic single-site solution.
- **FluentValidation NuGet** — three validated fields; framework overhead not justified when DataAnnotations + CommunityToolkit cover the need.
- **Manual `INotifyDataErrorInfo` implementation** — ObservableValidator provides the entire INotifyDataErrorInfo surface; hand-rolling duplicates it.
- **Value converter to null for enum** — converting `Sex.Unknown` to `null` before binding to force `[Required]` to fire is indirection; `[CustomValidation]` is direct and self-documenting.

---

## Consequences

+ Per-field validation surfaces immediately on edit via `ValidatesOnNotifyDataErrors=True`.
+ Polish error messages live inline in VM source — consistent with single-locale UX rule.
+ Save flow (sprint-08) calls `ValidateAll()` before persisting; guarantees bulk-check even if user never touched a field.
+ `Sex_HasError_WhenUnknown` test documents and enforces the `[CustomValidation]` contract.
- Validation does NOT fire at construction — `HasErrors == false` on a fresh VM even with empty required fields. Callers relying on `HasErrors` without first calling `ValidateAll()` will get a false negative. Documented via `HasErrors_IsFalse_WhenJustConstructed` test.
- Polish error-message strings live in VM source; localization deferred indefinitely (single-locale app per project brief).
