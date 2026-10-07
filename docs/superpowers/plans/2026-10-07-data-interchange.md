# Data Interchange Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline, with test-first implementation.

**Goal:** Implement legacy timetable/period conversion, selective sharing and durable data
replacement while excluding direct native desktop behavior changes.

**Architecture:** Pure Core parsers produce immutable candidates. Desktop adapters own file
I/O and the existing ProfileSession/ProfileRuntime commit/publish boundary. The current
Tests project supports an explicit Core-only mode; no fourth project is introduced.

**Tech Stack:** .NET 10, System.Text.Json, existing xUnit and WPF test project.

**Spec:** ../specs/2026-10-07-data-interchange-design.md

## Global constraints

- Preserve original text and legacy files; validate before commitment.
- Keep profile writer v5 and existing .stwbackup/.stwpreset formats unchanged.
- No changes to XAML, tray, keyboard/IME, autostart or Application Clock.
- JSON import/export limit: 4 MiB UTF-8, depth 16; invariant native time precision.
- No Desktop/WPF dependency in Core; three projects remain.

## Review focus

- Duplicate JSON keys must fail even when their values are identical.
- Malformed schedule must reject a valid timetable in the same file.
- Unselected components and other semesters must remain unchanged.
- Failed/stale imports must not publish or create partial disk state.
- File bounds apply to encoded UTF-8, including export, and source bytes remain unchanged.

## Task 1: Core-only verification and legacy conversion

Files: modify Tests csproj; create Core Features/DataInterchange/LegacyTimetableImporter.cs,
LegacyImportCandidate.cs, DataJson.cs; add Tests/DataInterchange/LegacyImportTests.cs.

Produces: `LegacyTimetableImporter.Import(string, string?) -> LegacyImportCandidate`,
with immutable Timetable, Schedule and Reports (Code, Path, Message).

- [ ] Enable Core-only tests and run existing pure tests as a baseline.
- [ ] Add tests for lossless text, missing/default reports, normalization and invalid JSON.
- [ ] Run tests and observe missing converter failures.
- [ ] Implement complete strict conversion and rerun the Core-only suite.

## Task 2: Selective sharing

Files: Core Features/DataInterchange/TimetableDataPackage.cs, TimetableShareFile.cs;
Tests/DataInterchange/TimetableSharingTests.cs.

Produces: package `ApplyTo(SemesterSet, bool, bool) -> SemesterSet`;
`TimetableShareFile.Export(TimetableDataPackage) -> byte[]`, `Import(byte[]) -> package`.

- [ ] Add failing selection, exact round-trip, schema and size-bound tests.
- [ ] Implement package selection and strict .stwshare v1 codec.
- [ ] Run all Core-only tests, including prior conversion tests.

## Task 3: Files and durable replacement

Files: Desktop Features/Persistence/TimetableDataFiles.cs;
ProfileSession.cs, ProfileRuntime.cs; Tests/Persistence/TimetableDataImportTests.cs.

Consumes: Task 1 converter, Task 2 package/codec.
Produces: bounded read-only file operations and atomic export;
`ProfileSession.ImportData(package, reviewedSnapshot, bool, bool) -> string?`;
matching ProfileRuntime method with persist-before-publish and one refresh.

- [ ] Add failing tests for failure/retry, stale baseline, disk/runtime agreement, isolation
  and unchanged source files. Use existing real TEMP store with fault injection.
- [ ] Add adapters and existing-session commit integration without native UI changes.
- [ ] Compile the full solution and run full Windows tests via CI.

## Task 4: Verification and handoff

Files: .github/workflows/data-verification.yml, docs/DATA-INTERCHANGE.md,
docs/adr/0025-data-interchange-foundation.md; update README, FEATURE-MAP, CONTINUITY,
DEVELOPMENT, ARCHITECTURE and ADR index.

- [ ] Record Linux Core-only results and full Windows CI results with exact scope.
- [ ] Review the whole diff for contract, data safety and unexpected native changes.
- [ ] Fix material findings with regression tests; run final checks.
- [ ] Commit and create a draft PR, preserving main and the existing CI setup PR.

## Execution notes

User requested autonomous development on 2026-10-07 after reviewing priorities and
verification boundaries. Continue inline without repeated design/step confirmations.
The cloud checkout is dedicated to this chat; work on codex/data-interchange.
