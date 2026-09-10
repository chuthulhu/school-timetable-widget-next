# ADR 0006 — Structured single-cell in-memory editing

Status: **Accepted**

Date: 2026-09-10

Decision status: **APPROVED — user-approved Editing Foundation UX, followed by
explicit SubjectText/ClassText scope adjustment**.

## Canonical value and presentation

`TimetableCell` keeps SchoolDay + PeriodNumber identity and one immutable
`TimetableCellValue` containing `SubjectText` and `ClassText`. Both are non-null
plain strings. Empty, Unicode, newline, leading/trailing whitespace and
whitespace-only are valid. No trim, normalization or semantic parsing occurs.
The value has no slot/date/UI identity and can be reused by future bulk input or
DateOnly overrides without inventing providers/importers now.

Core's old Content string API is removed. Desktop `DisplayText` is a one-way
projection, not an editable field or a compatibility shim. Default display is
SubjectText + newline + ClassText when both are nonempty. If either is exactly
empty, use the other alone; whitespace-only is not empty. Existing line breaks
in either field are preserved, so two fields can occupy more than two visual lines.
Display text is never parsed back to reconstruct fields.

Future legacy import carries the entire original Content into SubjectText with
ClassText = empty. Existing single-text development/test fixtures are migrated
that way explicitly; no runtime legacy importer or string overload is added.

## Editing contract

- Double-click or F2 on the selected/target body cell opens one owned modal
  single-cell editor. Keyboard focus identifies selection; its dashed outline is
  separate from the clock-driven background highlight.
- Opening copies both current fields into separate 교과/반 Draft inputs.
  Draft has no main-view preview and cannot mutate the original immutable value.
- Both inputs accept multiline plain text. Enter inserts newline; Tab traverses
  교과, 반, 적용, 취소. There is no default Apply button or added Apply shortcut
  such as Ctrl+Enter. Ordinary focused-button activation remains standard WPF.
- Explicit **적용** validates/constructs one value, updates exactly the captured
  target cell with both fields atomically and closes on success. **취소**, title-bar
  X and Escape discard both Draft fields. Reopening uses latest accepted values;
  Cancel cannot undo a previous Apply.
- This milestone's committed state is an **in-memory accepted snapshot only**.
  UI states that changes last until the app exits. M2's durable Save/restart
  contract remains future work. No persistence or saved-success message exists.
- No bulk editing, Date Override, date display, Settings or application undo/redo
  system is implemented. Standard local TextBox editing behavior is not a new
  application history system.

## Ownership and success boundary

`CellEditSession` knows only original value, label and a typed commit callback.
It does not know WeeklyTimetable, date, current time, highlight or global state.
`WeeklyTimetableEditor` adapts a captured slot/current immutable cell and allows
one active session per weekly owner. `WeeklyTimetableViewModel` owns the accepted
weekly snapshot and stable 35 cell presentations. No MainWindowViewModel/AppState,
new project, generic provider hierarchy or persistence service is introduced.

`WithCellValue` validates before creating a complete immutable weekly snapshot.
The owner replaces the snapshot once; cell VM updates its value and projection
before notifications, so observers never see half of a Subject/Class pair. A
same-value Apply is a no-op. Other 34 cells and the original snapshot are unchanged.
Terminal sessions cannot reapply; target-rejected Apply retains both Drafts and
an error. The callback contract requires rejection before modification. This is
not a persistence transaction or crash-recovery implementation.

UI entry captures the focused/double-clicked cell, never the highlighted cell or
its text. The existing single DispatcherTimer continues during the modal editor.
Apply may change content measurement; the Windows minimum helper is explicitly
refreshed after successful editing, including when values are cleared. Highlight
continues to change background only. Selection outline changes opacity only.

## Future constraints

DateOnly timetable and period schedule overrides will be independent and preserve
base data. Future effective day configuration will be resolved using the same
clock snapshot and shared by Status/Highlight/Notification. CurrentDateText
(`yyyy년 MM월 dd일`) will be separate from CurrentTimeText (`HH:mm:ss`) and share
that snapshot. Future rectangular Bulk Input will reuse canonical values and an
all-or-nothing commit boundary. Details are recorded in ARCHITECTURE/FEATURE-MAP;
no override, bulk parser, schema, date selector or date-display code is added here.

## Evidence

See [Timetable Editing Foundation](../TIMETABLE-EDITING-FOUNDATION.md). Legacy's
whole-grid dialog, merge heuristics and swallowed write failures are not templates.
ADR 0003 remains the Settings transaction contract, distinct from this dialog's
Apply-and-close behavior. WPF object/event tests do not establish native
keyboard, IME, focus, double-click or title-bar input behavior.
