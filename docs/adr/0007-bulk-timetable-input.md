# ADR 0007 — Bulk timetable input

Status: **Accepted**
Date: 2026-09-10
Authority: user's explicit Bulk Timetable Input milestone request.

## Decision

Implement two explicit import modes, School and Canonical, with a shared text
parser, immutable complete WeeklyTimetable candidate, separate preview and explicit
atomic in-memory Apply. The domain remains unaware of spreadsheet sources.
Core Features/TimetableImport owns format recognition; Desktop owns session,
preview and target adaptation; Infrastructure/Windows owns OS clipboard access.
No libraries, projects or generic importer hierarchy are introduced.

Canonical is exactly 8 rows by 11 columns: exact headers and ordered literal 1–7.
Extra rows/columns, missing/duplicate/reordered periods and altered headers reject.
School searches the whole table for one 1–7 repeated five times signature.
Repeated signatures reject with a request to copy one region. An immediately
preceding weekday row, if present, must agree with Monday–Friday seven-column
blocks (merged blanks or repeated weekday labels). Missing weekday evidence is
allowed only with an explicit preview notice and user candidate selection.

School body is consecutive two-row pairs immediately below the period header.
A pair uses an explicit 교과/반 marker outside the region or populated teacher/
number metadata above an empty continuation. Recognized identity header names
are 교사/교사명/성명/번호, searched above the data, outside the 35 columns.
Names label candidates only; subject or class vocabulary is never used.
Two isolated rows without pair markers may be offered with a warning requiring
explicit selection and mapping confirmation. A second populated identity row,
odd body row count or unsupported multi-pair structure rejects the entire input.
No row skipping, single-row subject guessing or automatic candidate choice.
The complete pair policy and conservative limitations are documented in the
bulk milestone record; the user authorized conservative detection decisions.

Headers alone may use trim comparison in School mode. Actual SubjectText and
ClassText are never trimmed, normalized or reverse-parsed from display text.
Clipboard TSV recognizes quoting, doubled quotes, embedded tabs/newlines,
trailing empty columns and one optional final record separator. Rectangular
validation happens after parsing. Empty final records are not silently discarded.

## Amendment — shorter school days (2026-11-02)

A real school export had Wednesday ending at period 6: its header read 1–7, 1–7, 1–6,
1–7, 1–7 (34 columns), and School import rejected it. The user reported this and asked
for that format to import. School now accepts five contiguous day groups. Each group
counts 1, 2, … up to at most 7, and the next group restarts at 1. A day with fewer
periods imports its missing periods as empty slots, and the evidence names the day,
for example "수 6교시까지".

Shapes that are not a Monday–Friday header of at most seven periods still reject:

- A sixth group.
- An eighth period.
- Fewer than five groups.
- A start inside a longer numbered run.
- A broken sequence.

The weekday-row check follows the detected group widths. Candidates are still never
auto-selected, so the user sees the empty slots in the preview before Apply. Canonical
import is unchanged.

## Commit boundary and UX

A session captures the current immutable week and holds no write until Apply.
Selection creates a separate read-only preview. A failed read/parse clears any
old candidate so stale successful data cannot be applied. School candidates
require explicit selection plus confirmation of weekday and subject/class mapping.
Cancel, X and Escape discard the session. Apply replaces all 35 values and their
projections before notifying observers; stable cell controls and IsCurrent remain.
Stale target or active cell editing blocks Apply before mutation. One UI dispatcher
owns mutations; reentrant commits during publication are rejected.

Context menu provides School import, Canonical import and template copy. Ctrl+V
is scoped to the timetable view and enters School import; the separate cell editor
keeps ordinary TextBox paste. Import mode never changes by heuristics. Preview
states that all 35 active slots will be replaced until app exit. Copy is an explicit
user command; tests and development samples do not overwrite the OS clipboard.

Persistence, profiles/groups, date override, semester sets, Settings and small
rectangular paste are deferred. ADR 0003 remains the separate Settings contract.
Object/event verification is not native clipboard, keyboard, IME or visual evidence.
