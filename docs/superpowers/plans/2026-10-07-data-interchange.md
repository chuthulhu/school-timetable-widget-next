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

- [x] Enable Core-only tests and run existing pure tests as a baseline.
- [x] Add tests for lossless text, missing/default reports, normalization and invalid JSON.
- [x] Run tests and observe missing converter failures.
- [x] Implement complete strict conversion and rerun the Core-only suite.

## Task 2: Selective sharing

Files: Core Features/DataInterchange/TimetableDataPackage.cs, TimetableShareFile.cs;
Tests/DataInterchange/TimetableSharingTests.cs.

Produces: package `ApplyTo(SemesterSet, bool, bool) -> SemesterSet`;
`TimetableShareFile.Export(TimetableDataPackage) -> byte[]`, `Import(byte[]) -> package`.

- [x] Add failing selection, exact round-trip, schema and size-bound tests.
- [x] Implement package selection and strict .stwshare v1 codec.
- [x] Run all Core-only tests, including prior conversion tests.

## Task 3: Files and durable replacement

Files: Desktop Features/Persistence/TimetableDataFiles.cs;
ProfileSession.cs, ProfileRuntime.cs; Tests/Persistence/TimetableDataImportTests.cs.

Consumes: Task 1 converter, Task 2 package/codec.
Produces: bounded read-only file operations and atomic export;
`ProfileSession.ImportData(package, reviewedSnapshot, bool, bool) -> string?`;
matching ProfileRuntime method with persist-before-publish and one refresh.

- [x] Add failing tests for failure/retry, stale baseline, disk/runtime agreement, isolation
  and unchanged source files. Use existing real TEMP store with fault injection.
- [x] Add adapters and existing-session commit integration without native UI changes.
- [x] Compile the full solution and run full Windows tests via CI.

## Task 4: Verification and handoff

Files: .github/workflows/data-verification.yml, docs/DATA-INTERCHANGE.md,
docs/adr/0025-data-interchange-foundation.md; update README, FEATURE-MAP, CONTINUITY,
DEVELOPMENT, ARCHITECTURE and ADR index.

- [x] Record Linux Core-only results and full Windows CI results with exact scope.
- [x] Review the whole diff for contract, data safety and unexpected native changes.
- [x] Fix material findings with regression tests; run final checks.
- [x] Commit and create a draft PR, preserving main and the existing CI setup PR.

## Execution notes

User requested autonomous development on 2026-10-07 after reviewing priorities and
verification boundaries. Continue inline without repeated design/step confirmations.
The cloud checkout is dedicated to this chat; work on codex/data-interchange.

## Execution evidence — 2026-10-07

- Task 1 implemented: existing pure baseline 215 passed; new legacy conversion tests
  first failed compilation for the absent API, then passed with exact text/default reports.
- Task 2 implemented: absent package/codec failures observed before implementation;
  new Unicode edge cases exposed incomplete-surrogate handling and writer normalization.
  Runtime-constructed surrogate cases now verify strict rejection. Core-only suite: 299
  passed, zero failures/skips; build warnings/errors zero.
- Task 3 implemented and full Windows-targeted build passed on Linux. Independent review
  corrected schedule-only notification and store-lease restart mistakes in the tests.
  Initial Windows CI exposed a locked-file exception-type assumption; the assertion now
  accepts IOException or UnauthorizedAccessException and still checks original bytes and
  temporary cleanup. No production change was needed for this correction.
- Task 4 code/docs/CI workflow and independent review completed. Automatic approval review
  initially rejected branch upload for missing specific remote-export authorization; the
  user subsequently supplied that authorization, conditional on security review.
- Ruling: retain the three projects and add CoreOnlyTests mode rather than a fourth test
  assembly — preserves ADR 0005 project count; Windows defaults remain full coverage.
- Follow-up: user authorized GitHub upload/push/updates after security review. Changed-file
  and commit-diff credential/privacy checks passed, origin matched the connected owned repo;
  branch uploaded and draft PR #2 created without merging main or modifying PR #1.
- Verified commit 904aa668ab62a71c13b642820503dd2d7a0b2a6b: GitHub Actions run
  [37587681532](https://github.com/chuthulhu/school-timetable-widget-next/actions/runs/37587681532)
  passed Linux Core 299 and full Windows 1,263 tests (zero failures/skips), Windows build
  warnings/errors zero, and 18 isolated bootstrap cases. Native desktop validation and
  user-facing commands remain deferred. Canonical evidence: CONTINUITY-VERIFICATION.md.
