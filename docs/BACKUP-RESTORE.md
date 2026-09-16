# Full profile backup / restore

Status: **AUTOMATED VERIFIED — USER NATIVE UX APPROVED** (2026-09-15).
Authority: Full Profile Backup / Restore, Recovery Required, and Degraded-Origin Restore Failure
Policy approvals. [ADR 0018](adr/0018-profile-backup-recovery-required.md).
Start: main, HEAD/origin/main `10c3413de5a0676d8d059abc9e1c6e85e78a52da`, clean.
All changes remain uncommitted. Commit/push remain intentionally pending.

## User and file contract

`.stwbackup` contains `backupFileVersion: 1`, independently of contained `profileSchemaVersion: 4`
and `.stwpreset` version 1. Preset files share one display design; full backups preserve the
current committed profile. Export never Applies Drafts or saves profile.json.

Included: 35 SubjectText/ClassText cells, seven complete periods, date timetable/schedule
overrides, lunch, display configuration and all user presets with stable IDs/names/settings.
Font selection contains source, family and stable identity only. Missing System/Online fonts
retain identity, show fallback and can be explicitly downloaded later. Restore never downloads.
Excluded: current time/status/countdown/highlight, viewed week, machine/account/cache paths,
font binaries and transient data. User-entered text is preserved exactly, not scrubbed.

The external input/output bound is 4 MiB. Full validation uses the strict profile-v4 converter:
malformed/missing/duplicate/unknown fields, invalid schedules/dates/display/fonts/libraries or
dangling references reject the entire candidate. Old v1/v2/v3 profile readers produce current
canonical backups without rewriting the source. No legacy backup importer or selective merge.

Export writes a unique same-directory temporary file, flushes/closes and moves over the chosen
destination. Active app-owned profile/recovery/cache destinations are protected. Restore reads
its source file only, then previews counts/style/lunch and availability of all referenced fonts.
Explicit confirmation says current data will be replaced; cancellation performs no mutation.
Windows Save/Open dialogs use the dedicated extension and distinct Korean names.

## Transaction and ownership

ProfileBackupActions owns dialogs/confirmation, ProfileSession gates commitment,
JsonProfileStore owns durable replacement/recovery, and ProfileRuntime updates existing feature
owners before notifications. A modal/draft guard prevents restore alongside editing. The viewed
week remains unchanged; timetable, schedule, overrides, lunch, display/library and status refresh.
The app has no new global state framework, legacy import, failed-candidate history or timer.

Normal restore first secures one validated `profile.pre-restore.json`. A small
`recovery-required.json` record stores version, source hash, origin kind and pending stage;
source paths are fixed app-owned names. The marker is secured before candidate replacement.
Successful candidate save, runtime publication and refresh precede marker removal.
Publication failure attempts whole rollback; rollback failure preserves the marker/source and
blocks edits/saves/import/restore re-entry. Startup checks the marker before ordinary profile
loading, even when current profile.json is valid. No silent acceptance or automatic retry.

Explicit recovery verifies the preserved source/hash, writes safely, publishes and refreshes,
and only then removes the marker. Missing/invalid evidence keeps recovery blocked. No cleanup
of pending evidence is permitted. Existing corrupt-original copies are retained after success.

## Approved degraded-origin distinction

Corrupt/unsupported startup has temporary defaults, not a healthy previous user profile.
Before explicit backup restore, preserve its exact original bytes as
`profile.recovery-original.json`, with hash, without parsing/normalizing/repairing those bytes.
If candidate publication and rollback both fail, enter Recovery Required.

Explicit **복원 전 상태로 되돌리기** retries the preserved exact original and verifies bytes.
Success clears the recovery transaction but returns to labeled degraded defaults with normal
persistence still blocked. It is not described as healthy data recovery. **데이터 복원...**
becomes available again without requiring restart. A later successful valid backup restore
returns to writable normal mode. Retry failure retains marker/original and blocking, without
automatic retries or defaults overwriting user data. The selected backup source remains unchanged.

## Resolved P2 — invalid backup input

ProfileBackupActions now handles the file contract's `InvalidDataException` separately at the
restore UI boundary. Malformed, unsupported-version and semantically invalid files produce a
short Korean message and return without preview, snapshot creation or profile/runtime mutation.
File access failures retain their separate guidance. The boundary does not catch arbitrary
exceptions: a regression test confirms an unexpected `InvalidOperationException` still escapes.
Normal, degraded and Recovery Required state/evidence remain unchanged, and a valid file can be
selected and restored immediately after an invalid attempt. Source bytes remain unchanged.

## Verification and WPF investigation

Initial codec/recovery-file tests: 34 passed. The initial full suite was 863/864, with the
existing SyntheticLoadedEventAppliesMeasuredContentMinimumWithoutShowingWindow failure inside
PackagePart.IsStreamClosed/CleanUpRequestedStreamsList during compiled XAML loading.
Log: `%TEMP%/stw-backup-foundation-tests.log`.

The unchanged failing test passed alone (1/1) and in its class (8/8). A bounded diagnostic
invoked the same existing test from six parallel callers, ten times each. It reproduced the
same package-resource stack. Log: `%TEMP%/stw-wpf-concurrency-probe.log`. This isolated the
failure to concurrent WPF test resource lifetimes; the production app composes its views on
one UI dispatcher. It is not evidence of a production backup data defect.

All existing WPF dispatcher helpers now route through a shared test-only resource-lifetime
gate covering construction, assertions and Dispatcher shutdown. Expectations are unchanged;
no test was skipped/deleted/weakened. The bounded concurrent-caller probe is retained as a
regression test. After isolation, the original test passed alone (1/1), its class passed 8/8,
and the concurrent regression passed 1/1.

Restore transaction/failure coverage includes consecutive previous snapshots,
viewed week, four atomic candidate write failures, serialization/snapshot/publish stages,
actual runtime exception plus rollback failure, normal/degraded explicit recovery and retry,
restart prioritization, invalid/inaccessible recovery material, recovery-copy failure and active
Draft guards. Backup/restore targeted tests passed 65/65 before final additions; focused restore/UI
tests subsequently passed 34/34.

Earlier automated gate: `dotnet restore`, `dotnet build --no-restore` and the full minimal-verbosity
suite passed: **900/900**, failed/skipped 0, build warnings/errors **0**. Object/event tests cover
preview, entry availability, cancel, invalid input, retry and source immutability. Tests use unique
TEMP profiles without native input, production files or system clock changes. Self-audit found no
font bytes/cache paths/viewed week/derived state, profile write from backup, automatic download,
partial restore, merge, silent candidate acceptance, recovery evidence cleanup, legacy importer,
multi-profile/history framework or outstanding backup/restore P1/P2 at that gate.

## Native review and screen-fit follow-up — 2026-09-15

Using one isolated TEMP profile, the user confirmed normal backup/preview/cancel/restore,
restored data after restart, degraded warning/write blocking, degraded restore to normal editable
state and a second restart without the warning. Disk inspection matched the restored profile to
the backup payload, retained the normal pre-restore snapshot, retained the exact 29-byte corrupt
original under its expected SHA-256, removed the recovery marker and left source backup bytes
unchanged. All owned runs exited normally with empty stdout/stderr logs. Navigating to the exact
diagnostic TEMP path was awkward; this is recorded as a test-path constraint rather than evidence
that the ordinary Windows dialog default location is wrong.

Native review then found a multiline cell clipped after editing. The stored subject contained a
trailing newline and the class remained a separate value; no backup corruption or normalization
occurred. The cause was a synchronous content-minimum measurement that saw the previous WPF
binding height, followed by OS work-area capping that compressed the seven equal rows.

The approved screen-fit policy now defers and coalesces actual layout measurement, grows/shrinks
relative to a retained preferred height, caps against the current monitor work area using its
actual DPI, minimally corrects bottom overflow, and enables a body-only vertical scrollbar only
at that cap. Object tests cover fit/grow/no-scroll, cap/scroll/period-7 reachability, shrink,
manual minimum, work-area change, position correction, DPI conversion, no tick resize and restore
publication with multiline/display changes. The user then confirmed the complete native policy:
full three-line rendering, grow without unnecessary scroll, work-area cap with body-only scroll,
period 7 access, natural shrink, resize without clipping, fixed top headers, stable positioning and
restart persistence. The user explicitly approved this UX. The repeated final gate passed
**907/907**, failed/skipped 0, with build warnings/errors **0**.

## Local placement exclusion — approved 2026-09-16

Machine-local window-state.json is explicitly excluded from .stwbackup. Restore leaves the
current PC's preferred width/height/position/monitor hint unchanged, including degraded or
Recovery Required workflows. Restored content may increase or shrink measured minimum and
applied geometry; it cannot replace preferred geometry. The existing export destination guard
also protects the local window-state file. No backup/profile schema change. See
[ADR 0019](adr/0019-machine-local-window-placement.md) and [Window Placement](WINDOW-PLACEMENT.md).
