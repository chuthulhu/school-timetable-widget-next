# Project continuity

Tool-independent handoff entry point. Snapshot date: 2026-10-07.
Read this after [README](../README.md), then follow [Development](DEVELOPMENT.md).
This repository contains the implementation and authoritative project documentation.

## Authority and reading order

1. [Product Contract](PRODUCT-CONTRACT.md): approved product meaning and invariants.
2. [Accepted ADRs](adr/README.md): design decisions, including explicitly superseding follow-ups.
3. Current source and tests: evidence of what is actually implemented.
4. [Architecture](ARCHITECTURE.md), [Persistence](PERSISTENCE.md) and feature documents.
5. This current-state snapshot: navigation and verified baseline, not a replacement contract.
6. [Git history](PROJECT-HISTORY.md): why/when changes landed.

A contract/code discrepancy must be reported and resolved explicitly; tests alone cannot
approve a product change. Dated foundation records describe their original milestone:
later approved decisions supersede earlier exclusions. Historical “commit pending” or
native checkpoint text is evidence history, not a current continuation requirement.
[Legacy Golden Reference](LEGACY-REFERENCE.md) is immutable behavior/evidence reference,
not an implementation template. AI conversations, session memory and private notes are
not authoritative and are not needed to build or resume development.

## Verified implementation baseline

### Data interchange foundation — 2026-10-07

User instruction: continue development while excluding direct native desktop behavior
changes. [Data interchange](DATA-INTERCHANGE.md) and [ADR 0025](adr/0025-data-interchange-foundation.md)
add raw legacy timetable/time conversion with supplementation reports, strict `.stwshare`
v1, selected Base replacement, bounded source reads and atomic export, and one reviewed
snapshot commit before runtime publication. **User-facing commands/dialogs are deferred.**
Profile v5, existing backup/preset formats and all native window/tray/input/OS code remain
unchanged. Full five-file legacy migration and teacher/group implementation remain deferred.

Local Linux evidence: Core-only baseline 215 tests; current suite 299 passed, 0 failed/skipped.
Full Windows-targeted source also compiled on Linux, warnings/errors 0. A Core-only run does
not establish Desktop transactions, Windows runtime or native input/rendering. Full Windows
integration/bootstrap results are pending the data-verification CI run.

The sandbox originally denied VSTest local sockets; the same test assembly's xUnit
in-process runner succeeded. With network permissions VSTest also ran. NuGet's initial
audit warning cleared after a force restore routed through the inherited proxy. No audit
suppression, system network/clock changes or native desktop launch was used.

### Previous accepted KRISS milestone — 2026-09-29

KRISS implementation (2026-09-29): **IMPLEMENTED / AUTOMATED AND NETWORK VERIFIED**,
limited native UX accepted. Base checkout: clean main/origin/main
`1438e163c2b2b8fbba3ef8b1c3b4093987a46ec0`. That implementation had 1,161 passing tests
(78 new), zero failures/skips and build warnings/errors. Official endpoint readback and three
production-parser NTP responses succeeded in the actual-user execution boundary; default
sandbox UDP timed out. Evidence and next actions: [KRISS time sync](KRISS-TIME-SYNC.md).
User native acceptance was explicitly received; the isolated app exited normally and final
production inventory/autostart readback was unchanged. Committed-checkout evidence is in
[Continuity verification](CONTINUITY-VERIFICATION.md); app push precedes registry refresh.

| Item | Value |
| --- | --- |
| Repository / development branch | chuthulhu/school-timetable-widget-next / main |
| Verified implementation commit | da3aedca70bd4e5668ffdf7e331a62b34f743733 — feat: add KRISS application clock synchronization |
| Product maturity | IN_DEVELOPMENT; local Windows desktop application, no hosted service |
| Stack | WPF, .NET 10 LTS, CommunityToolkit.Mvvm 8.4.2 |
| SDK selection | global.json: 10.0.400, latestFeature, stable only |
| Profile writer / supported readers | schema v5 / strict v1–v5; v1–v4 upgrade in memory, next successful save writes v5 |
| Standalone preset | .stwpreset, presetFileVersion 1, one user display preset |
| Full backup | .stwbackup, backupFileVersion 1; new exports embed profileSchemaVersion 5; embedded v4/v5 accepted |
| Machine-local files | window-state.json v1, tray-state.json v1; OS autostart registration separately |
| Automated baseline | 1,161 passed, 0 failed/skipped; build warnings/errors 0 |
| Baseline native evidence | KRISS UX accepted 2026-09-29; [scope and limitations](KRISS-TIME-SYNC.md); previous Semester Sets evidence retained |

The implementation SHA deliberately identifies the last application change, not the
self-referential SHA of this documentation commit. Use `git rev-parse HEAD` for the checked-out
documentation revision and `git log -1 -- README.md docs/CONTINUITY.md` for its handoff commit.
The platform project registry pins the subsequently verified/pushed handoff revision.
[Continuity verification](CONTINUITY-VERIFICATION.md) records this milestone's fresh-checkout gate.

## Current capabilities and source entry points

| Capability | Implementation / evidence |
| --- | --- |
| 35 independent Mon–Fri × seven cells, lossless Subject/Class text | Core Features/Timetable; [editing](TIMETABLE-EDITING-FOUNDATION.md) |
| School/Canonical bulk input, strict parsing, preview, atomic Apply, template copy | Core/Desktop Features/TimetableImport; [bulk input](TIMETABLE-BULK-INPUT.md). Captured active-semester Base target only |
| Editable complete seven-period schedule | Core Features/Periods and Desktop Features/PeriodScheduleEditing; [period editing](PERIOD-SCHEDULE-EDITING.md) |
| Current Status Header, five states, countdown, date-aware highlight | Core/Desktop Features/CurrentStatus and Core Time; one shared Application Clock snapshot per refresh; initial PC fallback then KRISS-derived KST, [sync](KRISS-TIME-SYNC.md) |
| Independent date timetable and date period overrides, optional lunch | Core Features/SchoolDays, Desktop Features/DateOverrides; [date overrides](DATE-OVERRIDES.md) |
| Previous/next week and independent per-date columns | Desktop Features/Timetable; [week navigation](WEEK-NAVIGATION.md). Actual status remains tied to today |
| Display customization and named user presets | Desktop Features/DisplaySettings; [display settings](DISPLAY-SETTINGS.md). Preview/Apply/OK/Cancel and stable preset IDs |
| Bundled/system/online fonts | Features/Fonts, Infrastructure/Fonts and Windows/FontLibrary; [catalog/licenses](FONT-CATALOG.md). Explicit download, local rendering, safe fallback |
| One-preset import/export | DisplayPresetFile and display Draft library; [preset format](PRESET-IMPORT-EXPORT.md) |
| Full-profile backup/restore, degraded mode, Recovery Required | Features/Persistence and Infrastructure/Persistence; [backup/recovery](BACKUP-RESTORE.md) |
| Multiline screen fit, preferred placement/size | Infrastructure/Windows/WindowContentMinimum, WindowPlacementController; [placement](WINDOW-PLACEMENT.md) |
| Tray show/hide, explicit Exit and per-user/session single instance | Features/TrayLifecycle and Infrastructure/Windows; [lifecycle](TRAY-LIFECYCLE.md) |
| Explicit Windows autostart | Features/Autostart, WindowsAutoStartRegistrationStore; [autostart](AUTOSTART.md) |
| Semester Sets | Core Features/Semesters, Desktop Features/Semesters and ProfileSnapshot; [semester ownership](SEMESTER-SETS.md) |
| KRISS synchronization | Desktop Infrastructure/Time, WindowsResumeSignal and App composition; official NTP, no system clock mutation or persisted correction; [ADR 0024](adr/0024-kriss-application-clock-sync.md) |

Repository paths in this table are relative to the corresponding project under `src/`.
Tests mirror the feature areas under `tests/SchoolTimetableWidget.Tests/`.
`App.xaml.cs` is the composition/lifetime entry; MainWindow composes feature views.
Core remains independent of WPF/Toolkit/Desktop. Tests reference Core and Desktop and run on Windows.

## Persistence boundaries

| Boundary | Owned data |
| --- | --- |
| Portable profile | Ordered Semester Sets, stable semester IDs/names and active ID; each semester's base timetable, base schedule and independent date overrides; global display settings, user presets/font identities, global lunch option |
| Machine-local convenience | Preferred window placement/size, tray notice receipt, Windows autostart OS registration, downloaded font cache |
| Local recovery evidence | Pre-restore snapshot, preserved corrupt original and recovery-required marker; safety material, never a disposable cache |
| Derived/runtime only | Current time/status/countdown/highlight, effective projections, viewed week, hidden tray state, applied geometry, Draft/Preview and font availability |

Production profile location is `%LOCALAPPDATA%/SchoolTimetableWidget/profile.json`.
A whole backup contains portable profile inputs only: every semester and the active ID.
It excludes window/tray state, autostart, font bytes/cache and browsing state.
A preset file contains one design and logical font references, not timetable/profile data.
See [Persistence](PERSISTENCE.md), [ADR 0019](adr/0019-machine-local-window-placement.md),
[ADR 0021](adr/0021-per-user-windows-autostart.md) and [ADR 0022](adr/0022-semester-ownership.md).

## Safety contracts to preserve

- Validate the complete candidate and **persist before publish**. Failed Apply retains prior
  commitment and retryable Draft. [Persistence](PERSISTENCE.md), [ADR 0003](adr/0003-settings-transaction.md).
- No partial restore or silent acceptance. Preserve recovery evidence; marker-first startup
  and explicit recovery are required after double failure. [Backup/Restore](BACKUP-RESTORE.md).
- Corrupt is not missing: preserve original bytes, use labeled degraded defaults, block writes.
  A degraded-origin rollback returns to degraded state, not a claim of healthy recovery.
- **Close != exit; hide != suspend.** X/Alt+F4 hides; tray Exit terminates; hidden refresh
  continues. Elect a single instance before profile/UI startup. [Lifecycle](TRAY-LIFECYCLE.md).
- Feature code must not read DateTime.Now/DateTimeOffset.Now directly. Only the PC fallback
  adapter reads wall time; consumers share one snapshot. [ADR 0004](adr/0004-application-time-source.md).
- Preserve legacy source unchanged. Do not vendor legacy implementation/history.
  [ADR 0001](adr/0001-golden-reference-policy.md).
- Machine-local UI/OS state is not portable profile state. Restore cannot change autostart
  or preferred geometry. Missing fonts preserve logical identity.

## Future teacher profiles and groups — deferred decision

**DEFERRED — FOUNDATION READY (2026-09-29); NOT IMPLEMENTED.**
[Teacher Profile / Group Foundation](TEACHER-PROFILE-GROUP-FOUNDATION.md) and
[ADR 0023](adr/0023-teacher-profile-group-ownership.md) now settle stable opaque
ProfileId/GroupId, many-to-many ProfileId references and logical teacher timetable ownership
by (ProfileId, SemesterId). School/semester schedules should not be duplicated per teacher.

The current product is still implicit single-teacher. Semester Sets remain completed;
profile v5, backup v1, ActiveSemester semantics and all user-visible behavior are unchanged.
Physical nesting, selection hierarchy/UX, global versus per-profile active semester and
future migration/backup compatibility remain deferred. No implementation authorization,
schema v6, migration code or speculative types are introduced by this foundation.

Foundation verification (2026-09-29): `dotnet restore`, `dotnet build --no-restore` and
`dotnet test --no-build --logger "console;verbosity=normal"` exited 0; 1,083 passed,
0 failed/skipped, build warnings/errors 0. Changed-document local file/heading links and
`git diff --check` passed. Diff scope is README/docs/ADR only: production source, tests,
schema, importer and recovery code are unchanged. Self-audit found no outstanding P1/P2
or contract conflict within this scope. No native input/rendering verification was performed.

## Resuming safely

Future notifications must consume this same Application Clock. Their scheduling/dedup/resume/
delivery milestone remains separate. Teacher Profile/Group stays FOUNDATION ONLY / DEFERRED.

Read the contract/ADR for the feature, inspect current code/tests and check the working tree.
Follow DEVELOPMENT for restore/build/test; use its fresh-checkout procedure to detect hidden
local dependencies. Record methods precisely: automated WPF object/event tests are not native
keyboard/IME/rendering, OS login or all-monitor/DPI evidence. Historical native limitations,
including suppressed tray balloons and the semester final hash-comparison limitation, remain
in their feature evidence documents; no missing local log is needed to understand them.

Keep durable decisions/results in the repository, update the current Feature Map and this
snapshot when implementation changes, and retain important evidence before cleanup.
Build output is reproducible; unknown files, sole backups, production profiles, font/license
source assets and recovery evidence must not be treated as generated disposable output.
