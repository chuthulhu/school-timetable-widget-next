# ADR 0012 — Native local profile persistence

Status: **Accepted**. Date: 2026-09-11.
Authority: Local Persistence Foundation milestone and explicit A load-failure policy approval.
Implementation / verification: [Persistence](../PERSISTENCE.md).

## Decision

Persist one immutable, complete profile snapshot as versioned UTF-8 JSON using
System.Text.Json and storage-only DTOs in Desktop Infrastructure/Persistence.
Core retains immutable timetable/school-day values without JSON or file APIs.
The Desktop aggregate holds base week, base schedule, date entries and the lunch
presentation preference. It is not a generic global AppState or multi-profile UI.

Production location is Environment.SpecialFolder.LocalApplicationData followed by
SchoolTimetableWidget/profile.json. Never use the repository, executable directory
or working directory. Tests inject unique TEMP directories. Development samples
must use isolated temporary storage and cannot load or seed production data.

Schema 1 contains schemaVersion and profile; profile holds timetable (35 canonical
slots), periodSchedule (ordered 1..7), dateOverrides (unique weekday dates, complete
seven-cell and/or seven-period components), presentation.showLunchBetweenPeriods4And5.
Null text, missing fields, unknown fields, duplicate properties and invalid identities
reject the complete load. Text is never trimmed or normalized. Dates use yyyy-MM-dd;
times use invariant HH:mm:ss.fffffff to retain existing TimeOnly tick precision.
Snapshot order and JSON formatting are deterministic, UTF-8 without BOM.
Current date/time/status/highlight/countdown/effective results are never persisted.

Each successful user Apply/change follows validation → full snapshot → durable save
→ runtime publication → relevant shared refresh. Failed writes preserve committed
runtime and Drafts and surface a Korean error. Cancel/Preview/refresh/exit do not save.
Both date components use one save. Existing stale-target checks precede saving.

## Load failure — approved A policy (no longer deferred)

Missing file is first run: defaults and writes allowed. A supported, fully valid file
restores before constructing/showing MainWindow. Corrupt syntax/semantic validation
or unsupported schema uses clearly labeled temporary defaults with all writes blocked.
The app does not modify, rename, delete, overwrite or repair the original file, nor
restore a previous version. Apply/remove/lunch changes cannot publish in this mode.
Draft/Preview remain usable, with failure shown on Apply. A persistent notice states
load failed, temporary defaults, original unchanged, saving unavailable, and file path.
Access failures also fail closed; stack traces are restricted to debug diagnostics.
Closing only releases resources. Restart rechecks the file; no recovery UI is added.

This resolves the native-startup recovery deferral in ADR 0003 and Architecture.
Full P8 migration and P9 backup/restore recovery/format decisions remain deferred.

## File boundary

Acquire an exclusive profile.lock handle for the store lifetime before loading.
This is a minimal cooperating-writer guard, not the future single-instance activation
UX. Keep the expected file bytes and reject detected external changes before saving.
Serialize and validate completely, create a unique temporary file in the same
directory, write all bytes, Flush(true), close, then File.Move(temp, destination,
overwrite) as the final rename. No destination truncate, delete-then-move, copy
fallback, async save queue, or save at shutdown. Clean only the owned temp name.

.NET 10 Windows File.Move delegates to MoveFileEx; same-parent paths avoid its
cross-volume copy behavior. File.Replace was evaluated but Windows documents
partial rename failure states; its backup option is not needed for the approved
no-automatic-recovery policy. No previous file/history is created in this milestone.
This provides the tested same-volume replacement boundary; exhaustive power-loss,
hardware/filesystem failure and uncooperative external writers are not guaranteed.

Primary references inspected 2026-09-11:

- [.NET 10 Windows file implementation](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/FileSystem.Windows.cs)
- [FileStream.Flush](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0)
- [MoveFileExW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-movefileexw)
- [ReplaceFileW failure states](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew)

## Future boundaries

Teacher profiles/groups and semester sets remain PLANNED. The current profile object
can migrate into a future collection; it does not give timetable values global
identity or merge teacher timetable ownership with school-day schedule semantics.
Legacy conversion may later produce this validated snapshot and use the same save
pipeline. No legacy source, backup UI, Settings/font/clock customization, tray,
autostart, notification, installer or updater is implemented here.

## Schema 2 follow-up — 2026-09-11

[ADR 0014](0014-display-presets-and-schema-v2.md) adds required profile.display in v2.
A separate strict v1 reader injects Standard display defaults without a load rewrite;
the next successful user save writes v2. Existing durable inputs and atomic boundary
are unchanged. Old v1-only binaries may reject v2; downgrade support is not implemented.
