# Data interchange without desktop interaction

## Intent and scope

The user authorized development to proceed as far as practical while excluding changes
that directly alter native window, tray, keyboard/IME or OS integration behavior. Existing
P8/P10 contracts supply the data-safety requirements. Implement a tested data-processing
foundation for legacy timetable/period conversion and selective file sharing. Desktop
commands, dialogs, native input and full legacy five-file migration remain separate work.

## Decisions

- Keep the three-project solution. Add an explicit `CoreOnlyTests` mode to the existing
  Tests project: net10.0, Core reference only, an allowlist of pure existing/new tests.
  Default to it on non-Windows hosts. Windows default remains the full existing suite.
- `LegacyTimetableImporter.Import(string timetableJson, string? scheduleJson = null)`
  reads raw Korean-day/period-key JSON. Missing slots become empty Subject/Class values;
  the full original string goes to SubjectText with empty ClassText. Missing periods use
  approved defaults, with one report entry per supplementation. H:mm is normalized to
  HH:mm with a report. Wrong shapes, duplicates, extra keys, invalid/reversed/overlapping
  intervals reject the entire candidate. No filesystem or OS effects.
- `TimetableDataPackage` holds a timetable, schedule, or both; neither is invalid.
  `ApplyTo(SemesterSet, bool useTimetable, bool useSchedule)` replaces selected Base
  components only. Selection must be nonempty and present. Identity, name and date
  overrides are retained; unselected components keep their exact references.
- `.stwshare` v1 is a UTF-8 JSON envelope: `shareFileVersion: 1`, optional `timetable`
  (35 entries: schoolDay enum name, periodNumber, subjectText, classText) and optional
  `periodSchedule` (seven ordered entries: periodNumber, start, end). Native times use
  invariant `HH:mm:ss.fffffff` to preserve ticks. At least one component is required.
  Unknown/duplicate properties, missing fields, future versions and invalid complete
  values reject the entire file. Limit import/export to 4 MiB of UTF-8, JSON depth 16.
  The format contains no semester IDs, overrides, display, geometry, autostart or clocks.
- Parsing is separate from I/O. A Desktop file adapter reads bounded UTF-8 inputs without
  modifying sources, and writes shared files with same-directory flush/atomic replace.
- A reviewed import captures the current ProfileSnapshot reference. ProfileSession rejects
  it if that reference changes, validates the selected replacement and performs one existing
  durable commit. ProfileRuntime then publishes both values together and refreshes once.
  Save failure retains all prior references and permits retry against the same baseline.
  Existing editor sessions cannot overwrite replacement Base values because their captured
  references become stale. No new menu/dialog or change to native interactions is made.

## Alternatives

Full legacy migration now would require unresolved appearance/notification conversions.
Adding sharing dialogs now would require native input review. This foundation completes
the independently testable processing and transaction boundary before either extension.
No QR decoder, profile v6, teacher/group model or legacy implementation copy is introduced.

## Verification

Run existing pure tests as a baseline, observe new tests fail before implementation, then
run the Core-only suite on Linux and compile the full solution with Windows targeting.
Windows CI must run all existing and new integration tests plus bootstrap checks. Exercise
Unicode/whitespace/tick round trips, missing/default reports, duplicate/wrong JSON,
overlaps, partial selection, size bounds, read-only source preservation, failed commit,
stale previews, restart and cross-semester isolation. No desktop launch is required.

## Delivery status

This is backend functionality, not a claim that end users can select these operations from
the widget. Record automatic evidence separately from unperformed native validation.
