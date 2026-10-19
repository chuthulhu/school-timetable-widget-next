# Timetable data interchange

Status: **PROCESSING/TRANSACTION FOUNDATION IMPLEMENTED; USER-FACING UI DEFERRED**.
Scope: [ADR 0025](adr/0025-data-interchange-foundation.md), Product Contract P8/P10.
This feature does not yet add widget menus or import/export dialogs.

## Legacy timetable and periods

`LegacyTimetableImporter.Import(timetableJson, scheduleJson)` accepts the raw
`timetable_data.json` and optional `time_settings.json` formats from the fixed Legacy
Golden Reference. It does not accept full profiles, sharing envelopes or QR payloads.

- Timetable: Korean day keys 월/화/수/목/금, exact period keys `1`–`7`, string values.
  Original text goes entirely to SubjectText; ClassText is empty. No trim/split/merge.
- Every absent slot is supplemented as an empty cell, with a `MissingCell` report.
- An omitted schedule input or absent period uses the approved DefaultPeriodSchedule,
  with a `MissingPeriod` report for every period supplemented.
- Supplied periods require both start/end in H:mm or HH:mm. Single-digit hours receive
  a `NormalizedTime` report. Full seven-period order and non-overlap are validated
  after supplementation; invalid/reversed/overlapping intervals reject the whole input.
- Duplicate/unknown keys, wrong shapes, non-string text, comments and trailing commas
  reject the input. Raw `{}` is explicitly empty/missing data, with all supplements
  reported; another format is never treated as an empty successful timetable.

Reports have stable Code/Path and Korean Message fields. They are conversion evidence,
not permission to commit without a reviewed preview. Full five-file migration (styles,
notification preferences, geometry and legacy backup folders) remains planned.

## Selective file format

`.stwshare` v1 is UTF-8 JSON with no BOM on export. A UTF-8 BOM is accepted on input.
The root has `shareFileVersion: 1` and at least one of these components:

| Field | Complete content |
| --- | --- |
| timetable | Array of 35 entries with schoolDay (Monday–Friday exact enum name), periodNumber (1–7), subjectText, classText; each slot exactly once |
| periodSchedule | Array of seven entries in period order, with periodNumber, start and end in invariant HH:mm:ss.fffffff |

Native time precision is lossless down to ticks, unlike legacy HH:mm input. Each present
component must be complete and valid, including components later left unselected.
Missing/null fields, duplicates, unknown fields, unsupported versions, malformed UTF-8,
invalid slots/intervals and JSON deeper than 16 reject the entire file. Imports and exports
are limited to 4 MiB of encoded UTF-8. No implicit format detection is performed.

Files contain no semester ID/name, date overrides, display settings/presets, placement,
autostart, notifications or derived clock state. Full profile backups and display presets
remain separate formats. A receiving user explicitly selects timetable, periods or both;
an empty selection or selected component absent from the package is rejected.

## Ownership, I/O and commitment

`TimetableDataPackage.ApplyTo` makes an immutable replacement of selected Base inputs,
retaining the receiving SemesterId/name and existing date overrides. Unselected inputs
and other semesters are retained. Date overrides continue to win over Base data for their
dates; import does not silently erase those overrides.

`TimetableDataFiles.ReadLegacy`/`ReadShared` open explicit paths read-only, check length
before allocation and reject a file growing during the read. An explicitly supplied missing
schedule path is an I/O failure, not a request for defaults. Source bytes are never rewritten.
`ExportShared` validates the entire payload before same-directory temporary write, flush
and atomic replacement. The caller owns path/overwrite selection; no chooser is added.

Capture `ProfileSession.Current` when presenting the candidate. Pass that exact reviewed
snapshot to `ProfileRuntime.ImportData(package, reviewed, useTimetable, useSchedule)`.
Any intervening commit, including a semester switch, invalidates the preview. Do not rebuild
the reviewed reference at Apply time. Runtime blocks open display/cell edit sessions and
commits all selected inputs once before publishing both canonical values and refreshing.
Old period drafts are invalidated by their existing reference checks. Failed save publishes
nothing, retains disk/runtime/baseline and permits retry after the storage fault is resolved.

## Verification

Core-only tests cover original text, defaults/reporting, exact round trips, malformed inputs,
schema/size bounds, selection and preserved identity/overrides. Windows integration tests use
isolated TEMP data and real JsonProfileStore fault injection for persist-before-publish,
failure/retry, stale previews, cross-semester isolation, restart, read-only source bytes and
export failure. They do not launch native windows, inject input or mutate OS registration.
Execution results and CI links are recorded in [Continuity](CONTINUITY.md).
