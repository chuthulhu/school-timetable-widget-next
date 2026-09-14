# Display Preset Import / Export

## User contract

Display Settings can save one selected **내 프리셋** as a `.stwpreset` file and can preview
and import such a file. This shares display design only. It is not a profile backup and never
contains timetable, class/subject, period, date override, lunch, clock, machine path or user
account data. Built-in styles are always supplied by the app and are not export targets.

Export reads the selected preset from the current Draft preset library. Typography control
changes become part of that preset only after **현재 설정으로 업데이트**; the export tooltip
describes the source as the selected preset's saved current editing copy. Export creates only
the destination file and does not Apply, save the profile, activate a style or change cache.

## Format and validation

- Extension: `.stwpreset`
- Encoding/content: readable UTF-8 JSON without BOM; deterministic property ordering
- Version: top-level `presetFileVersion: 1`, independent of profile schema v4
- Maximum input/output: 64 KiB
- Payload: stable ID/name plus layout, Time/Date/Weekday/Status typography, 12/24-hour,
  seconds and date/weekday/status visibility
- Font identity: source + family ID + family name only; no binary, URL, registry or path

The importer rejects malformed JSON, duplicate or unknown properties, missing fields,
unsupported versions, invalid/non-D/empty IDs, invalid names/layout/typography/options,
unknown source kinds and unknown bundled/online catalog identities. Parsing has no side
effects. There is no partial recovery or automatic future-version migration.

## Preview, portability and collisions

The preview names the preset, layout, visibility/format choices and each element's family,
size and availability. Bundled fonts resolve locally. A missing System font remains a valid
logical identity and uses rendering fallback. A known Online font without valid cache also
retains identity, shows download-needed/fallback, and can be explicitly downloaded later.
Import itself never starts a download.

No collision adds the original stable ID. Same ID offers **기존 프리셋 업데이트**,
**새 복사본으로 가져오기**, or cancel; there is no overwrite-by-file-selection. Update keeps
the ID. Copy generates a new ID and requires a unique editable name. Name-only collision
suggests `이름 (복사본)` (numbered when necessary) for user confirmation. Built-in display
labels remain distinguishable through `기본 제공 ·` / `내 프리셋 ·` labels.

Successful import changes only the Display Settings Draft library and does not select or
activate the imported preset. Apply/OK performs the existing complete profile transaction.
Cancel/X restores the last successful Apply baseline. Save failure retains Draft import state
for retry while committed profile/display and disk remain unchanged.

Architecture decision: [ADR 0017](adr/0017-display-preset-files.md).

## Verification — 2026-09-14

Automated/model/WPF-object coverage includes deterministic Korean UTF-8 round trip, exact
ID/name/configuration, all three font references, excluded profile/private/path/binary data,
strict malformed/future/oversized/missing/duplicate/unknown/invalid cases, new/update/copy/
cancel/name collisions, built-in protection, Draft/Apply/Cancel/save-failure semantics,
non-activation, missing-online fallback and TEMP-profile restart restoration. File dialogs
are behind an injected boundary; unshown WPF tests verify button availability, preview and
collision choices without automating OS dialogs.

Final post-acceptance gate: restore succeeded; build succeeded with warnings/errors 0; **830/830**
tests passed (804 baseline + 26 new), failed/skipped 0; `git diff --check` reported no whitespace
error. Source/serialized-shape audit found no timetable/profile/machine/cache/font-binary field,
network/download call, schema-v5 change, auto-activation, partial library mutation, built-in
overwrite, same-ID silent overwrite or copy-ID reuse.

Native review used owned processes and unique TEMP profiles. A normal build was visible on the
user desktop. Read-only Win32/UI Automation inspection verified the main and Settings windows;
the real Windows export dialog saved `교무실 시계.stwpreset`, whose 1,261-byte UTF-8 payload had
only `presetFileVersion` and `preset`, preserved the ID/name/settings, and contained no excluded
work/profile/path/binary terms. Export did not create `profile.json`.

The available Computer Use surface exposed no native-app control API. Background UI Automation
could inspect the standard Open dialog and set its native edit value but could not activate its
confirmation; this is recorded as an input limitation, not an import failure. For the remaining
preview/collision review, a temporary DEBUG-only startup hook and injected fixed-path file dialog
were used and explicitly treated as a changed test condition. The production import parser,
session transaction and preview window were otherwise exercised unchanged. Native inspection
confirmed no-collision preview, four font statuses, no automatic activation, no disk change before
Apply, same-ID update/copy choices, copy-name proposal/reconfirmation, new copy GUID, and built-in
selection retained after Apply. A fresh cache-free TEMP profile showed `Orbitron / 42` as
`다운로드 필요 · 지금은 기본 글꼴로 표시`; only `profile.lock` existed during preview, so no
download/cache/profile write occurred. The temporary hook and diagnostics were removed before the
final gate. The user reviewed the visible preview and explicitly approved the UX on 2026-09-14.
