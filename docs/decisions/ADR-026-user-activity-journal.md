---
id: ADR-026
title: Per-day user activity journal separate from the diagnostic log
status: Accepted
date: 2026-06-09
---

## Context

The Python implementation writes one human-readable line per user action to a per-day file alongside the diagnostic log. The file records which button was clicked and which person was loaded at the time, in plain text without log-level noise. This audit trail helps the user and a support person diagnose sequences of operations without reading a technical log. The .NET port had no equivalent.

A concern raised during design: if the journal write were placed directly inside each command method body, every future command would need to remember to add a journal call, and the concern would be scattered across fourteen separate methods. Forgetting a call in one place silently drops history.

## Decision

A journaling proxy wraps each command of interest at construction time rather than at invocation time. The proxy intercepts execution, writes a single line to the per-day journal file, then forwards the call to the underlying command. The journal file is placed in the same runtime directory as the diagnostic log, using a `__journey.log` suffix so it is easy to distinguish. The file path is resolved at call time from the current root so it follows root changes without requiring a restart.

Journal writes are unconditionally wrapped in a try-catch. Any write failure is logged at error level in the diagnostic log and the application continues normally. The journal file being absent or unwritable must never affect application behaviour.

The proxy implements the same command interface as the wrapped command so mode-gating wiring continues to work without modification.

## Consequences

- Every journaled command logs one line without any code in the command body knowing about the journal.
- Adding journal coverage to a new command requires only construction-time composition, not a code change in the method body.
- The per-day file is human-readable and can be opened in Notepad alongside the diagnostic log.
- A missing or corrupt journal file has no visible effect on the user.
