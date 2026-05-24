# ADR-004 — PersonViewModel validation strategy

**Status:** Accepted
**Date:** 2026-05-25

---

## Problem

Tab 1 (Dane osoby) requires validation on the Płeć field with a Polish error message. Validation must trigger on edit (per-field, live) AND on explicit save-time bulk check. Key trap:

1. `Sex` enum — `Unknown = 0` is the default value. `[Required]` on an enum silently passes the default (0 is not null; `Required` allows it). So `[Required]` alone does NOT enforce "user must pick a sex".
2. CommunityToolkit.Mvvm generated setters only call `ValidateProperty` when a value is assigned — construction with defaults does NOT fire validation. A fresh VM has `HasErrors == false` even though Sex is still Unknown.

Name fields (FirstName, LastName) are **optional**. Empty values are coerced to `"(nieznane)"` at persist time in the mapper — no validation attribute is needed on the VM.

---

## Decision

- `PersonViewModel` inherits `ObservableValidator` (CommunityToolkit.Mvvm).
- Only `Sex` carries `[ObservableProperty]`, `[NotifyDataErrorInfo]`, and `[CustomValidation(typeof(PersonViewModel), nameof(ValidateSex))]` — the source generator emits a setter that calls `ValidateProperty` on every change.
- `Sex` validated with `[CustomValidation(typeof(PersonViewModel), nameof(ValidateSex))]` — static method returns `ValidationResult("Płeć jest wymagana")` when `Sex == Sex.Unknown`.
- Name fields (`FirstName`, `LastName`) carry only `[ObservableProperty]` — no validation attribute. Empty values become `"(nieznane)"` in `MeFileMapper.ToMeFile()`.
- Public `ValidateAll()` delegates to `ValidateAllProperties()` — called explicitly at save time (sprint-08) to bulk-validate all fields regardless of whether setters fired.
- XAML bindings use `ValidatesOnNotifyDataErrors=True` for live visual feedback (WPF default red-border; no custom ErrorTemplate this sprint).

---

## Rejected

- **`[Required]` on `Sex`** — does not trip the enum default value 0 per `System.ComponentModel.DataAnnotations` spec. Would silently allow `Sex.Unknown` through as valid.
- **`[Required]` on `FirstName` / `LastName`** — names are optional; unknown persons have `"(nieznane)"` as a sentinel value, not a hard error. Validation would block saving records with legitimately unknown names.
- **Custom `[SexRequiredAttribute : ValidationAttribute]`** — one usage site; 3-uses rule fails; `[CustomValidation]` is the idiomatic single-site solution.
- **FluentValidation NuGet** — one validated field; framework overhead not justified when DataAnnotations + CommunityToolkit cover the need.
- **Manual `INotifyDataErrorInfo` implementation** — ObservableValidator provides the entire INotifyDataErrorInfo surface; hand-rolling duplicates it.
- **Value converter to null for enum** — converting `Sex.Unknown` to `null` before binding to force `[Required]` to fire is indirection; `[CustomValidation]` is direct and self-documenting.

---

## Consequences

+ Per-field validation on Sex surfaces immediately on edit via `ValidatesOnNotifyDataErrors=True`.
+ Polish error message lives inline in VM source — consistent with single-locale UX rule.
+ Save flow (sprint-08) calls `ValidateAll()` before persisting; guarantees bulk-check even if user never touched Sex.
+ `Sex_HasError_WhenUnknown` test documents and enforces the `[CustomValidation]` contract.
+ Empty name fields are silently coerced to `"(nieznane)"` in `MeFileMapper.ToMeFile()` — no error shown to the user.
- Validation does NOT fire at construction — `HasErrors == false` on a fresh VM even with Sex still Unknown. Callers relying on `HasErrors` without first calling `ValidateAll()` will get a false negative. Documented via `HasErrors_IsFalse_WhenJustConstructed` test.
- Polish error-message string lives in VM source; localization deferred indefinitely (single-locale app per project brief).
