# ADR-024 — Per-mode command availability matrix

**Status:** Accepted
**Date:** 2026-06-08
**Extends:** ADR-003

---

## Problem

The application operates in three distinct work modes — Add, EditTree, EditDraft — that map to fundamentally different user intents. Early versions kept all action buttons enabled in every mode, allowing a user to e.g. trigger "save to tree" while in draft-editing mode or "update draft" while in add mode. These invalid operations either silently did nothing or produced confusing state.

The target user is an elderly relative with limited digital experience. Presenting buttons that do nothing or cause unexpected transitions is a usability failure.

---

## Decision

Each action category is explicitly restricted to the modes where it is meaningful:

**Add mode (new person, purple tint)**
- Save new person to tree — enabled
- Save as draft — enabled
- Navigate to add mode — always enabled (self-loop allowed, resets form to blank)

**EditTree mode (editing a tree person, cyan tint)**
- Save changes back to tree — enabled
- Navigate to add mode — always enabled

**EditDraft mode (editing a staged draft, lime tint)**
- Overwrite the loaded draft — enabled
- Promote draft to tree — enabled
- Navigate to add mode — always enabled

**Always enabled in all modes**
- Open a tree person for editing (transitions to EditTree)
- Load a draft for editing (transitions to EditDraft)
- Set root person for generation commands
- Generate folder tree shortcuts
- Generate lineage folders
- Validate tree consistency
- Navigate to add mode / create new person

**Mode transitions**
- Creating a new person (Add → save) → transitions to EditTree with the saved person loaded
- Editing a tree person (EditTree → save) → stays in EditTree
- Saving as draft (Add → save as draft) → transitions to EditDraft with the draft loaded
- Overwriting a draft (EditDraft → update) → stays in EditDraft
- Promoting a draft (EditDraft → promote) → transitions to EditTree with the promoted person loaded
- Any mode → navigate to add mode → transitions to Add and clears the form

---

## Rejected

- **All buttons always enabled, mode determines silently which path runs** — confusing for the target user; button label does not reflect what it will do when pressed.
- **Full command-per-button split** — would require splitting three existing commands into eight, touching all existing tests; rejected in favour of CanExecute gating on the existing commands.

---

## Consequences

+ Each button is grayed out when its action is not applicable to the current mode — prevents invalid operations without error dialogs.
+ "Navigate to add mode / new person" is never disabled — user can always escape from any stuck state.
+ Mode indicator (header label + background tint) and button availability are consistent — the user can read which actions are available from the tint alone.
- CanExecute state must be refreshed whenever mode changes; missing a NotifyCanExecuteChanged call will silently leave a button in the wrong enabled state.
