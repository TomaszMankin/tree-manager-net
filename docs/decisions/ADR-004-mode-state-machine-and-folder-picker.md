# ADR-004 — Mode state machine and folder-picker library choice

**Status:** Accepted
**Date:** 2026-05-24

---

## Problem

App needs 3 work modes (Add / EditTree / EditDraft) with window-background tint distinguishing them — mirrors py-tree-manager's visual affordance users rely on.
Also: WPF in .NET 6 had no native folder picker, forcing Ookii.Dialogs.Wpf or WinForms FolderBrowserDialog. Must verify .NET 10 status before adding third-party dependency.

---

## Decision

**Mode model.** Enum `AppMode { Add, EditTree, EditDraft }` in `App/ViewModels/AppMode.cs`. Single mutable property `MainViewModel.CurrentMode` via CommunityToolkit `[ObservableProperty]`. Switch via `[RelayCommand] SwitchMode(AppMode)`. No state-machine library — 3 unconstrained modes; property assignment is sufficient.

**Background tint binding.** `MainWindow.xaml` `Grid.Background` bound to `CurrentMode` through `ModeToBackgroundBrushConverter`. Three `SolidColorBrush` resources in `App.xaml` (`AddModeBrush=#F3E5F5`, `EditTreeModeBrush=#E0F7FA`, `EditDraftModeBrush=#F9FBE7`). Converter resolves by `Application.Current.TryFindResource(key)` — colors live with theming, not logic.

**Folder picker.** Use BCL `Microsoft.Win32.OpenFolderDialog` (`PresentationFramework.dll`, available since windowsdesktop-8.0, confirmed on .NET 10). Wrapped behind `IRootPickerService` seam for L0 testability of the startup bootstrap flow.

---

## Rejected

- **State machine library (Stateless, etc.)** — 3 unconstrained modes don't justify a dependency. Single property assignment covers all transitions.
- **Hardcoded hex in converter** — colors duplicated between converter and any future theme would drift. Resource keys centralize the palette.
- **Ookii.Dialogs.Wpf** — redundant; framework provides the dialog natively on .NET 8+. Zero gain.
- **WinForms FolderBrowserDialog** — pre-.NET 8 fallback; superseded by WPF-native dialog.
- **Custom RootPickerDialog.xaml** — 100+ lines to recreate the OS dialog. No benefit.
- **Mode persistence across launches** — app starts in `Add` per py-tree-manager parity; no mode persistence.

---

## Consequences

+ Zero new NuGet packages.
+ `IRootPickerService` seam lets bootstrap flow be L0-tested with Moq; dialog impl stays a thin wrapper.
+ Brush resources are theming-ready without converter changes.
- `IValueConverter` needed (small ceremony) versus a direct `Grid.Style` `DataTrigger`; converter generalizes when other surfaces need the same tint.
- `Application.Current` dependency in converter means converter tests need an `ApplicationResourceFixture` (one xUnit class fixture).
- Bootstrap tests cover decision branches only; sprint-20 L2 test validates "user picks folder → pointer file written" end-to-end on a real desktop session.
