# ADR 0009 — Editable committed base period schedule

Status: **Accepted**
Date: 2026-09-10
Authority: user's Period Schedule Editing Foundation request and explicit chronological-order decision.

## Decision

A committed base schedule has exactly seven immutable periods, numbered 1 through 7
in that order. Each Start < End, and each previous End <= next Start. Gaps and
touching intervals are valid; overlap and reversed chronological order are not.
No automatic reorder or renumbering. Existing general Core resolver support for
unordered/partial calculation inputs remains unchanged; this stricter invariant
belongs to the user-committable base schedule value.

Desktop opens an owned modal seven-row HH:mm (24-hour) editor from the timetable
context menu, without adding a toolbar row. Period numbers are read-only. Draft
strings may be invalid while typing and never change runtime values. Apply parses
all rows, validates a complete candidate, replaces one immutable schedule, triggers
one immediate shared refresh and closes on success. Invalid Apply keeps the dialog
and Draft with a Korean period/field or adjacent-period error; no partial commit.
Cancel/X/Escape discard Draft. This is Apply-and-close, not Settings P2.

App composition owns the initial default and a small runtime base-schedule state.
Each refresh reads one clock snapshot and one schedule snapshot and derives date,
time, status, countdown and highlight from them. The editor does not know clocks,
resolvers, countdown or highlight. An explicit application callback requests the
shared refresh after successful replacement. No generic event bus or repository.

Period schedule and 35-cell timetable content are separate concerns. Core owns
schedule invariants; Desktop owns Draft/parsing/errors/dialog/runtime composition.
Future effective date configuration may resolve a different immutable schedule
from the same application date; no Date Override implementation is added now.

Persistence is absent: restart restores defaults. Date/font foundation is retained;
font manager, Settings, tray, notifications, installer and updater remain excluded.
Native review is required before the user-authorized commit and normal fast-forward
push to origin/main. See [verification record](../PERIOD-SCHEDULE-EDITING.md).
