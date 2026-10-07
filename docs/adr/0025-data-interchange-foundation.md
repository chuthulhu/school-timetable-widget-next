# ADR 0025 — Data interchange without native desktop changes

Status: **Accepted — processing/transaction foundation; user-facing integration deferred**

Date: 2026-10-07

## Context

The user requested continued development while excluding changes that directly alter
native desktop behavior. P8/P10 already require safe legacy conversion and selective
time/timetable sharing. Existing Tests require Windows for the full suite.

## Decision

Implement pure Core legacy timetable/time parsing and a strict `.stwshare` v1 codec.
Desktop owns bounded read-only source access, atomic export and a reviewed-snapshot
commit/publish adapter. Only the active semester's selected Base components are replaced.
No UI command, XAML, tray, native input, autostart, clock or profile schema changes occur.

Raw legacy timetable strings remain whole SubjectText values with empty ClassText;
missing cells/periods and time normalization are reported. Unsupported/wrong/duplicate
input rejects a complete candidate. Full five-file legacy profile migration remains planned.
`.stwshare` fields, precision and validation are documented in [Data interchange](../DATA-INTERCHANGE.md).

Retain the three projects of ADR 0005. Extend Tests with `CoreOnlyTests=true`: net10.0,
Core reference and explicit source allowlist. Non-Windows defaults to this mode. Windows
keeps the complete net10.0-windows suite. This supersedes only the Windows-only test
target in ADR 0005's follow-up, not its project count or production dependency rules.
CI runs both modes and the separate bootstrap suite; Core-only results are labeled as such.

## Consequences

Candidates can be converted, round-tripped and durably applied with automated tests and
without interfering with the user's desktop. End-user preview/selection/dialog integration
is still required before these operations are described as available widget features.
Profile v5, `.stwbackup` v1 and `.stwpreset` v1 remain unchanged. Source reading does not
change OS registration or infer teacher identities, class splits, spans or QR payloads.
