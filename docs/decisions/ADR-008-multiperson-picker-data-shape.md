# ADR-008 — MultiPersonPicker data shape: recompute-and-rebind

**Status:** Accepted
**Date:** 2026-05-28

## Problem

Family-tab pickers filter candidates by three independent inputs: search text, cross-role exclusions, own selection. Two WPF patterns viable: ICollectionView with filter delegate (incremental, textbook) vs computed list property re-raised on every change (full rebind). Choice locks the pattern for sprint-08 and sprint-09 pickers — consistency across pickers matters more than per-picker micro-optimisation.

## Decision

Candidates exposed as a computed, fully-recomputed list property re-raised on every relevant state change. L0 testability is the deciding factor: ICollectionView requires PresentationFramework and UI-thread pump, blocking headless tests; picker has enough conditional logic that L0 coverage is non-negotiable. Full recomputation acceptable at single-family scale (well under 1000 people). ComboBox SelectedItem resets on ItemsSource rebind — tolerable given type-then-click-Add interaction model.

## Rejected

- **ICollectionView with filter delegate** — not testable at L0 without WPF host; no measurable perf gain under 1000 entries.
- **ObservableCollection with incremental diff** — diff tracking across 3 filter dimensions adds complexity; recompute is simpler and equally correct at this scale.

## Consequences

- Good: full L0 coverage without WPF host; stateless, no filter-delegate lifecycle; pattern locked for sprint-08/09.
- Bad: full rebind per keystroke, perceptible past ~5000 entries (not current concern); SelectedItem reset on rebind (tolerable with Add-button model).
