# Semester Sets

Status: implemented; 1,083/1,083 automated tests passed; user native UX approved (2026-09-29).
Pre-milestone baseline: main and origin/main d36dc986fc8dd6392ac707588067f81393db6b2e, clean.
Implemented and accepted in ef6c977abbfdd9725183614701bb90bc3c03a5a8. Dated pending
checkpoints below are historical; current handoff is [Continuity](CONTINUITY.md).
Authority: user Semester Sets milestone and [ADR 0022](adr/0022-semester-ownership.md).

Semester data: stable ID, display name, 35 base cells, seven base periods, independent
DateOnly timetable and period overrides. Profile stores all semesters and a valid active ID.
Global: lunch presentation, display settings and presets/font selections.
Machine-local: window/tray/autostart, font cache and recovery state.

Switches are explicit, saved before publication, preserve viewed week and recompute today's
status/countdown/highlight from the Application Clock and selected instructional data.
New semesters activate only after a successful full save. Base periods always copy;
base timetable copies only with the default-OFF option. Date overrides never copy.
Rename retains identity; deleting an inactive semester requires confirmation. Active and
last semesters cannot be deleted. No dates, name interpretation or automatic activation.

Schema v5 replaces the single dataset with an ordered collection. v1-v4 input upgrades in
memory to 기본 학기, without startup rewrite. Backup envelope v1 is retained and new exports
embed profile schema v5. Full restore and recovery preserve all semesters and active ID.

## Verification gates

Completed automated gates: migration preservation/no rewrite, isolation and failure transactions,
fixed editor targets, import preview, global boundaries, coherent clock refresh, backup/recovery
compatibility, WPF object/binding tests, full restore/build/test/diff checks and self-audit.
The chronological records below distinguish automated evidence from the native acceptance scope.
Native smoke uses an isolated TEMP profile, direct launch first, and separately recorded user
visibility/UX acceptance. No native input or production-data mutation is authorized by an
unshown object test. Commit and normal fast-forward push follow user native approval only.

## Automated verification — 2026-09-18

`dotnet restore`, `dotnet build --no-restore`, and
`dotnet test --no-build --logger "console;verbosity=normal"` passed.
1,083/1,083 tests passed: 1,032 baseline cases retained and 51 new semester cases; no skipped
or failed tests. Build warnings/errors: 0/0. `git diff --check` passed.
Logs: `%TEMP%/stw-semester-restore.log`, `stw-semester-build.log`, `stw-semester-final-tests.log`.

Coverage: fixed v1-v4 migration/no startup rewrite/exact instructional data, v5 next save,
creation/copy/name validation/identity/order/restart, active/last deletion guards, semester
save failures, independent cell/period/import/date edits and same-date namespaces, pinned
stale editor targets with shared base values, viewed-week preservation, same-clock different
status/highlight, WPF selector binding and failed selection rollback through all four atomic
write stages, global state/machine-file preservation, old backup compatibility, all-semester
backup/pre-restore/rollback and Recovery Required restart/recovery.
The v4 fixture extends the fixed v3 fixture only with the v4 required System familyId fields.
Earlier tests retain original data checks but address the approved v5 JSON paths/version;
legacy field comparisons unwrap the one migrated semester rather than weakening preservation.

WPF evidence uses the existing shared resource-isolation gate and unshown objects/events.
No host keyboard, pointer, clipboard, system clock or production profile mutation occurred.
Actual native keyboard/IME, modal input blocking, visual layout and user UX acceptance remain
separate; passing object tests is not claimed as native rendering/input evidence.

## Self-audit before native checkpoint

No outstanding P1/P2 found in the reviewed implementation and automated scope. Semester
candidates validate before full atomic save, then owners publish/refresh. Failed switch restores
the selector to the committed ID. IDs are nonempty/unique; active exists; last/active delete
reject. New semesters never copy date overrides. Immutable copy data and captured semester IDs
prevent cross-semester retargeting. Other saves retain all inactive semesters and global data.

No date/month/name interpretation, ranges, week reset, sorting, preset/lunch duplication,
startup rewrite, partial load/recovery or active-only backup. v1-v4 readers and backup v1/v4
compatibility remain. Restore retains existing recovery semantics and excludes local machine
state. No semester registry/window/tray/font-cache calls, direct clock reads, new dependency,
multi-teacher model, MainWindowViewModel/AppState expansion or academic-calendar framework.
Commit/push remain gated on the user's requested native UX approval.

## Direct native launch — awaiting visibility

Codex directly launched the normal DEBUG app outside the sandbox using the previously verified
launch boundary, with `--effective-preview --bulk-preview` and a unique TEMP profile directory.
Owned PID: 44324. Process start/identity recorded in the diagnostic ledger; InputIdle true,
responsive and still running; stdout/stderr empty. MainWindowHandle was 0 on both process reads,
which is not evidence of visibility or an input failure. User visibility confirmation is pending.
No repeated launch or host input was performed.

Diagnostic directory:
`C:/Users/ADMIN/AppData/Local/Temp/stw-semester-native-c746c61131c24e279df4bd28178b669e`.
The normal production views use the existing synthetic Monday 2026-09-07 13:10 elapsed clock
and development import samples, explicitly identified by the title; storage is isolated.
Original production profile/window/tray/recovery existence, SHA-256 and mtime were captured
before launch and unchanged immediately afterwards. The script records owned process identity
and refuses duplicate relaunch while it is alive. Native input tools are disabled in this session;
no actual keyboard/IME, pointer, clipboard or OS-dialog interaction is claimed.

HEAD and origin/main remain d36dc986fc8dd6392ac707588067f81393db6b2e. Working changes are uncommitted;
no push or native UX approval has occurred. Next step is actual user visibility confirmation,
then the requested semester workflow/UX checkpoints and normal tray Exit/restart verification.

### Native visibility confirmed

The user replied “보임”, confirming the diagnostic widget is visible on their actual desktop.
This resolves visibility only; MainWindowHandle 0 did not imply a failed launch. No relaunch
was needed. Semester editing, switching, restart and backup/restore native checkpoints remain
pending. Next checkpoint: rename 기본 학기 to 2026 1학기 using the management UI.

### Native rename confirmed

User replied “변경됨” after renaming 기본 학기 to 2026 1학기 and checking the main selector.
TEMP profile.json is schema v5, with one semester named exactly 2026 1학기. Its stable ID and
ActiveSemesterId both remain 7b189e46-3dd5-4710-97bc-3a6e08e3d942; 35 cells, seven periods,
zero date overrides. A read-only diagnostic copy is saved as after-rename.json. This confirms
user-observed rename/selector display and durable identity/name, not later restart behavior.

### Native base cell edit confirmed

User replied “적용됨” after editing Monday period 1 in 2026 1학기. TEMP profile.json contains
SubjectText 물리 and ClassText 1-1 in that exact base slot. Active ID remains unchanged and
date overrides remain empty. Base period 1 is currently 09:00–09:50. This is user-reported
native Apply plus inspected durable data; cross-semester isolation/restart native checks remain.

### Native base period edit confirmed

User replied “적용됨” after changing only period 1 Start to 08:50. TEMP profile.json confirms
08:50:00.0000000–09:50:00.0000000 in the active 2026 1학기 base schedule. Monday period 1
물리 / 1-1 remains intact. Next checkpoint creates a date override before testing new-semester
copy exclusions.

### User-requested relaunch — 2026-09-21

User reported closing the program and requested relaunch to continue. The diagnostic launcher
verified no matching owned process remained and launched the same DEBUG executable, TEMP profile
and explicit effective/bulk preview options. New owned PID 41168, InputIdle true, responsive,
not exited; stderr empty. Original production file existence/hash/mtime still match the initial
snapshot. Prior process exit method/code was not independently observed. Actual visibility of
this new run is not inferred from process evidence. Before launch, the saved profile contained
2026 1학기 with zero date overrides; the next pending checkpoint remains 2026-09-07 timetable override.

### Native first-semester date override confirmed

User replied “적용됨” after the relaunched-app date edit. TEMP profile.json contains a complete
2026-09-07 timetable override whose period 1 is 1학기 특별수업 / 1-1, with no period-schedule
component. Base Monday period 1 remains 물리 / 1-1; base period 1 remains 08:50–09:50.
Saved first-semester-before-create.json as a diagnostic comparison snapshot before creation.
This records user-reported native Apply and exact persisted base/override separation.

### Native copy-OFF creation confirmed

User reported “빈시간표 확인”. Disk inspection confirms two distinct stable IDs; the newly active
semester has exactly 35 empty cells, zero date overrides and a base schedule exactly equal to
semester 1 (period 1 08:50–09:50). The complete first semester matches the pre-create snapshot.
The saved new label is literally 2926 2학기, rather than the instructed 2026 2학기. No name was
silently corrected; the next native step asks the user to rename it. This observation does not
establish an application input defect.

### Native second-semester label correction

User replied “수정함”. The active semester now has the exact display name 2026 2학기 and ID
30e68fe9-c02b-426c-970a-05adc6cb9596. The original 2026 1학기 retains ID
7b189e46-3dd5-4710-97bc-3a6e08e3d942 and remains inactive. Next checkpoint exercises the
existing development-sample canonical import preview with its explicit semester target label.

### Native canonical import into second semester

User confirmed the target label and Apply (“대상확인 적용”). Disk inspection confirms active
2026 2학기 (30e68fe9-c02b-426c-970a-05adc6cb9596) now contains the canonical development
sample: 35 cells, 30 nonempty, Monday period 1 SubjectText 물리학Ⅱ\n실험 and ClassText 1-1.
The complete first semester still equals first-semester-before-create.json. The second semester's
base schedule is unchanged and its date override collection remains empty. This establishes the
reported native target-label/Apply flow and persisted semester isolation, not a clipboard roundtrip.

### Native second-semester base schedule confirmed

User replied “적용됨” after changing 2학기 base period 5 Start. TEMP profile.json confirms
13:00–14:50 for that period. The full first semester still equals the pre-create snapshot;
second-semester date overrides remain empty. Next checkpoint creates a different timetable
override on the same 2026-09-07 date to verify the independent semester namespaces natively.

### Native same-date second-semester override confirmed

User replied “적용됨”. Both semesters now have independent 2026-09-07 timetable overrides:
period 1 is 1학기 특별수업 / 1-1 in semester 1 and 2학기 특별수업 / 1-1 in semester 2.
Semester 1 remains byte-equivalent at the canonical JSON object level to the pre-create
snapshot. Their base period 5 schedules differ as intended: 14:00–14:50 versus 13:00–14:50.
Saved two-semesters-before-switch.json for later comparison. Native visual switch, viewed-week
and status/highlight observations are the next checkpoint; they are not inferred from disk.

### Native switching, viewed week and status/highlight confirmed

User reported “정상확인 상단표시시각은 13:40분정도” after the requested 1학기/2학기 switching
checks. This confirms the observed same-date special-class changes, unchanged displayed week,
and at approximately synthetic 13:40 the first semester's Break/no highlight versus the second
semester's InPeriod(5)/Monday-period-5 highlight. The exact seconds/countdown value was not reported.
Disk inspection shows 2026 2학기 active and both complete semester datasets unchanged from
two-semesters-before-switch.json. Saved before-copy-on.json for the next create-with-copy check.

### Native copy-ON creation confirmed

User reported “복사 확인됨” after creating 복사 확인 with the timetable-copy option checked and
checking the base Monday-period-1 content instead of the source's date override. Disk confirms
three semesters and a new active ID fdb89868-b9ba-4582-af82-ddec1e533d0f. The new semester's
35 base cells and seven base periods exactly match the previously active 2026 2학기; its date
overrides are empty. Both original semesters are unchanged. Saved before-delete.json for the
upcoming active-delete protection and confirmed inactive-delete checkpoint.

### Native active-semester delete protection confirmed

User replied “삭제방지확인” after selecting the active 복사 확인 semester in management and
checking the disabled Delete action and guidance to activate a different semester first.
This records native UX protection/wording confirmation. Next step is explicit activation of
2026 2학기 followed by confirmed deletion of the now-inactive diagnostic copy only.

### Native confirmed inactive deletion

User replied “삭제됨” after switching to 2026 2학기 and confirming deletion of the inactive
복사 확인 diagnostic semester. Disk confirms exactly 2026 1학기 and 2026 2학기 remain, with
2026 2학기 active. Both complete original datasets exactly match before-copy-on.json; copy ID
fdb89868-b9ba-4582-af82-ddec1e533d0f is absent. Saved before-restart.json. The reply confirms
the deletion flow; no separate verbatim acknowledgement of the confirmation wording was given.
Next checkpoint is normal tray Exit and same-profile restart, with owned-process checks by Codex.

### Native same-profile restart — 2026-09-29

User reported “종료됨” after the requested tray Exit. Owned PID 41168 was absent (identity
checked against the ledger), and profile.json SHA-256 matched before-restart.json. Exit code
was not observed. The prior TEMP native.ps1 launcher was missing; that invocation failed
before starting an app. Codex then directly launched the existing DEBUG executable with the
same TEMP profile, --effective-preview and --bulk-preview options. New owned PID 27172 was
recorded, InputIdle true, responsive and not exited. Profile hash remained unchanged after
startup. Actual restored UI values await user confirmation. No force termination or input
injection occurred; this restart check does not independently establish unchanged production
files since the earlier recorded comparison.

### Native restart retention confirmed

User reported “재시작유지 확인” after checking initial 2026 2학기 selection and the respective
special classes while switching semesters. Disk inspection confirms both semesters and global
data match before-restart.json exactly; only ActiveSemesterId differs, currently 2026 1학기.
No failure is inferred from the final selected semester. The next instruction explicitly returns
to 2026 2학기 before exporting a full backup. Saved before-backup.json as a diagnostic snapshot
of the current state; its active ID is 1학기, not the intended upcoming backup selection.

### Native full backup export confirmed

User replied “백업됨”. semester-check.stwbackup exists (19,348 bytes), with backupFileVersion 1,
profileSchemaVersion 5, both semesters and active 2026 2학기. The complete embedded profile
matches the current persisted profile, including every semester and global field. SHA-256:
502B2147922F42875A1BF99A8E6E08CE056AB5E59FCB0748C130A5D77F399CCD.
Next checkpoint changes a diagnostic semester label before restoring this full backup, so both
collection replacement and active-ID restoration can be observed without deleting original data.

### Native pre-restore changes confirmed

User replied “변경됨”. Disk confirms semester 2 is now named 복원 전 변경 and the active
semester is 2026 1학기. Saved native-before-restore.json for comparison with the secured
pre-restore snapshot. Next checkpoint verifies backup preview semester count/active name and
explicit full restore, expecting the prior name 2026 2학기 and its active ID to return.

### Native full restore confirmed

User reported “미리보기 확인, 복원됨” after checking the preview's two semesters/current
2026 2학기 and explicitly restoring. The complete restored profile equals the backup payload,
including both semester datasets, original names, active ID and global settings. The secured
profile.pre-restore.json exactly matches native-before-restore.json, including the changed
label and active 1학기. No recovery-required marker remains. Source backup SHA-256 is unchanged:
502B2147922F42875A1BF99A8E6E08CE056AB5E59FCB0748C130A5D77F399CCD.
Native workflow checks have reached overall UX acceptance and normal diagnostic Exit; these
final user responses, owned-process cleanup, final validation and commit/push are still pending.


## Final native acceptance and verification — 2026-09-29

The user explicitly replied “종료, 승인”, approving the exercised semester UX and reporting
normal tray Exit. All owned process identities in the diagnostic ledger are now absent;
restart stderr/stdout files are empty. No forced termination was used; exit codes were not
captured. Native acceptance covers selector/management, rename, copy OFF/ON, base and date
content isolation, canonical development-sample import target/Apply, differing base schedules,
13:40 status/highlight switching and viewed-week retention, active-delete protection, confirmed
inactive deletion, restart retention, full backup preview/export/restore and restored active ID.
It does not establish every keyboard/IME/modal/DPI path, a real clipboard import, native fault
injection or a separately exercised date-period override. Those remain automated/source evidence
where covered. The ordinary UI used explicitly synthetic time and isolated TEMP storage.

Original production-file hashes/mtime were verified unchanged at the earlier recorded checks.
At final cleanup, original-state.clixml was missing, as the launcher had been on relaunch.
The first final comparison command therefore failed to read its baseline; its subsequent
identity-mismatch message was a consequence of the missing baseline, not observed different-user
execution. The original baseline was not reconstructed or replaced. Final end-to-end production
hash comparison cannot be claimed. The owned-process check was rerun separately and passed.

Fresh approved-run dotnet restore, dotnet build --no-restore and dotnet test --no-build --logger
"console;verbosity=normal" all exited 0: 1,083 passed, 0 failed/skipped; build 0 warnings/errors.
Logs: %TEMP%/stw-semester-approved-restore.log, stw-semester-approved-build.log,
stw-semester-approved-tests.log. Source changes are the same ones reviewed natively; subsequent
changes only document observations. Final self-audit found no outstanding P1/P2 in this scope.
The user's conditional ordinary main commit/push authorization is satisfied; no force push,
other branch, remote/system/credential change is authorized or required.

## Future teacher boundary — foundation only, 2026-09-29

Semester Sets remain **COMPLETED** and the current v5 ownership/ActiveSemester behavior is
unchanged. [Foundation](TEACHER-PROFILE-GROUP-FOUNDATION.md) and
[ADR 0023](adr/0023-teacher-profile-group-ownership.md) define future teacher timetable ownership
by (ProfileId, SemesterId), retaining stable SemesterId and school/semester schedule meaning.
This does not require refactoring today's valid single-teacher SemesterSet. Teacher Profiles /
Groups remain DEFERRED; physical nesting and selection design are future decisions.
