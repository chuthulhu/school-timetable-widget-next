# ADR 0022 — Semester ownership and active semester

Status: **Accepted**. Date: 2026-09-18.
Authority: explicit Semester Sets milestone request. Implementation and native approval are separate gates.

A profile contains an ordered, nonempty collection of immutable SemesterSet values and one
valid ActiveSemesterId. Stable nonempty GUIDs identify semesters; editable names do not.
Names are trimmed on input, nonblank, at most 80 UTF-16 code units, unique using ordinal
case-insensitive comparison. Collection order is creation order and persists unchanged.

Each semester owns its 35-cell base week, seven-period base schedule and independent date
timetable/schedule overrides. Lunch presentation, display and user presets remain global.
Font cache, window/tray state and Windows autostart remain outside semester ownership.

Switch, create, rename and inactive delete build and validate a full profile candidate,
atomically persist, then publish. Failure preserves the prior active ID and all data.
New creation also activates in that transaction: copy base periods, leave the week empty
unless explicitly requested, never copy date overrides. Rename preserves ID and data.
Delete requires confirmation; last and active semesters cannot be deleted. No fallback
activation, date ranges, automatic selection, name parsing, reorder or deletion history.

MainWindow exposes a compact selector and an owned management dialog. Modal editors block
normal selector input. Editors capture semester identity as well as provenance and reject
stale targets; shared immutable values alone cannot establish semester identity.
Viewed week persists in runtime across a switch. Actual status always uses Application
Clock today and the active semester's effective schedule, with one source snapshot per cycle.
Bulk import replaces only the captured semester's base week and names its target in preview.

Write profile schema v5. Strict v1-v4 readers wrap the original instructional data in one
neutral 기본 학기, retaining exact strings, periods, overrides and global settings. Migration
is in memory only; next successful profile save writes v5. No startup rewrite or partial load.

The backup envelope remains version 1; its embedded profileSchemaVersion distinguishes 4
and 5. Old v1 backups remain readable. New backups, pre-restore snapshots, restore and
rollback include every semester and the active ID. Existing Recovery Required behavior
and machine-local exclusions remain. Preset files are unchanged.

No multi-teacher/group model or academic calendar framework is introduced.
Evidence and remaining checks: [Semester Sets](../SEMESTER-SETS.md).
