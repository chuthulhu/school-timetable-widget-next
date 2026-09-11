# Local Persistence Foundation

Date: 2026-09-11. Status: **IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED (limited scope)**.
Authority: milestone request, explicit A load-failure policy approval and final user native acceptance.
Preparation/pending entries below are chronological history, superseded by the final acceptance sections.
[ADR 0012](adr/0012-native-local-profile-persistence.md).

## User behavior

앱을 종료했다 다시 실행해도 시간표와 일과 변경이 유지됩니다. 기본 시간표의 교과/반,
기본 일과, 날짜별 시간표/일과 예외와 점심 표시 옵션이 저장됩니다.
성공한 적용은 저장 후 화면에 반영됩니다. 저장하지 못하면 이전 내용이 유지되고
편집 창에서 오류를 확인하고 다시 시도할 수 있습니다.

A missing file is first run and writable. A supported valid file loads completely before
MainWindow construction. Invalid JSON/semantics/unsupported schema fail closed: temporary
defaults, persistent notice, original unchanged and all durable changes blocked.
Drafts and previews may still be inspected; Apply rejects without closing. Lunch also
keeps its old value and shows an error. No save occurs on Cancel/Preview/ticks/exit.

## Storage and scope

Production: `%LOCALAPPDATA%\SchoolTimetableWidget\profile.json`, resolved through .NET's
SpecialFolder API. Schema 1: `schemaVersion` plus `profile` containing `timetable`,
`periodSchedule`, `dateOverrides`, `presentation.showLunchBetweenPeriods4And5`.
Each cell records canonical weekday name, period identity, SubjectText and ClassText.
Dates are invariant yyyy-MM-dd; times are HH:mm:ss.fffffff preserving full TimeOnly
precision. JSON is indented UTF-8 without BOM. Strings round-trip without normalization.
Entries are ordered deterministically; schedules/day overrides are complete and validated.

The immutable snapshot holds only durable inputs, separate from JSON DTOs and mutable
UI Drafts. Neither current date/time nor status/highlight/countdown/effective projection
is serialized. Core files and existing calculation invariants remain unchanged.

A synchronous UI-dispatcher save writes the complete validated document to a unique file
in the same directory, Flush(true), close, then File.Move with replacement. First save
uses non-overwriting move. No destination truncate/copy/delete fallback. Owned temp files
are cleaned best-effort. profile.lock excludes cooperating second writers until disposal;
expected-byte checks reject detected external file changes. There is no automatic
previous-file restore, repair, rename or history. P6 activation UX remains future work.
See ADR for evaluated Windows APIs and durability limitations.

Full Backup/Restore, Legacy Migration, teacher profiles/groups, semester sets, date bulk
import, full Settings, clock/font presets, tray/autostart/notifications/installer/updater
remain PLANNED. No actual user's file is intentionally corrupted by verification.

## Automated evidence

Final dotnet restore, dotnet build --no-restore and dotnet test --no-build --logger
"console;verbosity=normal" all exited 0. **585 passed, 0 failed/skipped; build warnings/errors 0**.
Baseline 508 retained plus 77 new cases. SDK 10.0.401; Core/Desktop runtime 10.0.12.
Log: `C:\Users\ADMIN\AppData\Local\Temp\persistence-automated-48389bbbe73043e186ba5b0e46bf0b48.log`.
Tests use unique TEMP directories and private store lifetimes.
No production profile reads/writes, native keys/pointer/clipboard, system clock changes,
foreground dialogs or windows are involved. Unshown WPF object/binding tests establish
presentation wiring only, not actual native rendering/input.

## Native restart smoke — pending

Use DEBUG --dev-profile-directory=<absolute directory beneath TEMP> with the same path
for both launches. Preview flags automatically isolate storage; their sample seed applies
only when the file is missing. Normal production startup never seeds sample data.
Codex directly launches the built normal app first, then asks for actual desktop visibility.
User checks a cell edit, base period edit, date override, lunch ON, normal close/restart and
retention, followed by F2/import/status/highlight/date editor/resize regression checks.
Native acceptance and final pre-commit report are required; no commit/push is authorized yet.

Coverage includes exact empty/Korean/Unicode/newline/whitespace pair round-trips across
cultures; full schedule tick precision; all independent date components and multiple dates;
malformed/missing/null/duplicate/unknown fields, invalid slots/dates/schedules/options;
unsupported schema; defaults/writable vs degraded/blocked outcomes; unchanged original
bytes and modification times through load/rejected save/disposal; four injected write
failure stages, failed first writes, real Windows rename denial and successful retry;
exclusive writer and detected external-change refusal; all seven user commit paths with
success/failure, Draft retry and coherent disk/runtime; stale targets, immutable collection
copy, preview-seed isolation, restart/effective-day/header with fake clock, no saves for
Draft/Cancel/Preview/refresh, and unshown startup notice/menu binding checks.

The first rename-denial assertion was adjusted to accept the actual Windows
UnauthorizedAccessException as well as IOException, both already handled by production.
Three existing geometry tests were updated to resolve named controls and account for the
new collapsed notice row, preserving their original geometry/identity checks. The object
MenuItem.OnClick test drains through ApplicationIdle so the queued click command completes;
DataBind-only draining observed the intermediate checked state before command execution.
No tests were removed or skipped. These were test expectation/harness corrections.

## Self-audit before native review

No outstanding P1/P2 finding in the inspected source/automated scope. Reviewed the complete
new storage/snapshot/composition files and all changed editors/actions/views. Confirmed:

- Production location uses per-user SpecialFolder; no absolute user path or repo/exe writes.
- Core has no new JSON/file/UI dependencies and no derived state is in the storage DTO.
- Complete validation and immutable candidate precede any replacement; all user changes
  use the same ProfileSession pipeline and keep runtime/Draft on failed persistence.
- Old destination is never directly truncated/deleted. Injected failures and actual OS
  rename denial preserve its complete bytes; only the owned temporary name is cleaned.
- Invalid/unsupported loads are all-or-nothing; original bytes/mtime survive blocked
  Apply/lunch/disposal. No auto-repair/rename/restore or default overwrite exists.
- One writer lease; no background save queue, timer writes, exit save or Cancel save.
- No string Trim/Normalize, inferred merge/deduplication or partial date snapshots.
- Tests use unique TEMP directories, do not call production profile resolution and do not
  read/write the real user's profile. No intentional corruption of real user data occurred.
- Future profile/group/semester/backup/migration/Settings/font/tray/OS effects stay excluded.
- git diff --check passed. Native input/render/restart acceptance is still pending; no
  hardware power-loss or arbitrary external-writer guarantee is claimed.

## Direct native launch — awaiting user visibility

Codex directly launched the normal Debug executable outside the sandbox with
--effective-preview --bulk-preview and an explicit isolated TEMP profile directory.
Owned PID 30616, start 2026-09-11T08:44:04.9859811+09:00, input-idle true, running,
expected synthetic-time title and window handle 3083154 were observed. This proves
process/window existence only; actual user desktop visibility and native review await
confirmation. No native key/pointer/clipboard input was sent.

Storage and identity/log directory:
`C:\Users\ADMIN\AppData\Local\Temp\SchoolTimetableWidget-native-persistence-3299e5e884c84a56917069fc13fd94cc`.
Reuse this same directory on restart. It is independent of production LocalAppData.
The app is deliberately left running for the user checkpoint. No commit or push occurred.

### Relaunch and user visibility confirmation

At the user's request, Codex relaunched the same executable and isolated TEMP profile
outside the sandbox. The prior owned PID 30616 was no longer present; its shutdown
method/exit code was not observed. New owned PID 24568, start
2026-09-11T09:43:04.4826497+09:00, input-idle true, expected title and window handle
4851668 were observed. The user then answered `보임`, confirming actual desktop visibility.
This does not yet establish persistence retention or completion of native editing checks.

### Native base-cell Apply

The user answered `적용함` after instructions to edit Tuesday period 1 via F2 to
SubjectText `저장 확인` and ClassText `3-1`, then Apply. Codex read only the isolated
TEMP profile.json and verified schema 1 and that exact canonical slot/value pair on disk.
This establishes user-reported Apply and durable file contents; fresh-process restoration
remains pending. No production profile was accessed.

### Native base-period Apply

The user answered `적용함` after changing base period 5 to 13:00–13:50 in the period
editor. Codex verified those exact values in the isolated profile.json and confirmed
Tuesday-period-1 `저장 확인` / `3-1` was retained. This records user-reported Apply and
the complete file's relevant inputs, not an independently observed Header/highlight or
restart result. Date override and lunch persistence checks follow.

### Native combined date-override Apply

The user answered `적용함` after enabling both components for 2026-09-07, editing
period-1 SubjectText/ClassText to `날짜 저장` / `2-2` and date period 5 to 14:00–14:50.
Codex verified one date entry with complete seven-cell and seven-period components and
those exact inputs in the isolated profile.json. Base Tuesday-period-1 `저장 확인` /
`3-1` and base period 5 at 13:00–13:50 remained intact. This records user-reported Apply
and disk contents; native restart restoration is still pending.

### Native lunch preference change

The user answered `체크됨` after enabling the lunch context-menu option. Codex verified
presentation.showLunchBetweenPeriods4And5 is true in the isolated profile.json.
This records the user's checked-menu observation and the persisted preference. Normal
shutdown/relaunch with this same profile is the next checkpoint; no production data is used.

### Owned normal shutdown and persistence restart

Codex verified PID 24568's saved executable/start identity, requested CloseMainWindow
and observed process exit within ten seconds, remaining PID count 0. No force termination
or physical title-bar-X claim; ExitCode was unavailable (null), not asserted as zero.
The profile SHA-256 was unchanged across shutdown and fresh startup:
6D2C2C0D7BCB03FEE5B7EE9E09576FFCEF70F673F6B6754033B7FC3E0E3AB06A.

The same executable/arguments/TEMP directory were relaunched outside the sandbox.
New owned PID 42972, start 2026-09-11T09:47:58.2115822+09:00, input-idle true,
expected title, handle 5899210, running. Actual restored UI values/menu state await
user confirmation. No native key/pointer input or production profile access occurred.

### User screenshot confirms restored content and lunch presentation

The user supplied `C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-a316c6f5-09f3-423c-beb9-5fe43bfd3481.png`
after the persistence restart. It shows Monday-period-1 `날짜 저장` / `2-2`,
Tuesday-period-1 `저장 확인` / `3-1`, date 2026년 09월 07일, time 13:10:48 and
Header `점심시간 · 5교시까지 49분`. No current-cell highlight is visible, consistent
with the effective overridden schedule's lunch Break. This confirms the captured
restored base/date content and lunch presentation. The screenshot does not show the
lunch menu check itself or the base/date schedule editor fields; those are not inferred
as separately observed. Native base schedule inspection follows.

### User confirms restored base schedule

The user answered `유지` after being asked to inspect base period 5 in the period
editor for 13:00–13:50 following restart, then close with Cancel. This records
user-confirmed restoration of the base schedule fields. The separate date schedule
fields are the next native check.

### User confirms restored complete date override

The user answered `반영되어 있음` after instructions to reopen 2026-09-07 and verify
both component toggles enabled, period-1 `날짜 저장` / `2-2`, date period 5 at
14:00–14:50, then Cancel. This confirms the reported native restoration of the
complete date timetable/schedule components. Together with base-cell, base-period
and lunch presentation evidence above, the requested durable inputs have passed
this restart scenario. Remaining native regression/overall acceptance is pending.

### User confirms import preview regression

The user answered `미리보기 정상 표시 확인` after opening both School and Canonical
import entries with development sample input and instructions to Cancel. This records
normal native preview display in both modes. It does not establish a new clipboard
roundtrip or native bulk Apply; those transaction paths have automated coverage.
Current-period highlight and resize regression remain to be checked.

### User confirms current-period highlight and resize regression

The user answered `정상` after changing only date 2026-09-07 period-5 Start to 13:00
(End retained at 14:50), checking Header InPeriod(5), Monday-period-5 highlight and
horizontal shrink/expand presentation. Codex verified the diagnostic date interval
13:00–14:50 in the isolated profile.json. This records the stated limited native
status/highlight/resize acceptance; no all-DPI, monitor, pixel or timing claim is made.
Restoring this temporary date Start to 14:00 and overall native approval are next.

### User accepts native review and restores the diagnostic interval

The user answered `적용함. 승인` after restoring date 2026-09-07 period-5 Start to
14:00 and being asked for native approval. This explicitly accepts the reviewed native
scope. Codex verified the final isolated profile still contains Tuesday-period-1
`저장 확인` / `3-1`, base period 5 at 13:00–13:50, Monday-period-1 override `날짜 저장` /
`2-2`, date period 5 at 14:00–14:50 and lunch ON. The file's hash returned to the
pre-highlight-test hash. No production code changed during native review.

### Accepted-run normal shutdown

Codex verified PID 42972 against the saved executable and exact start time, requested
CloseMainWindow and observed exit within ten seconds, remaining PID count 0. No force
termination. ExitCode was unavailable (null), so exit code zero is not asserted.
All three owned native stderr logs were empty. Profile SHA-256 was unchanged before/after
shutdown: 6D2C2C0D7BCB03FEE5B7EE9E09576FFCEF70F673F6B6754033B7FC3E0E3AB06A.
The diagnostic profile remains in TEMP for review; no production profile was touched.

## Native evidence scope — accepted

The user confirmed actual visible launch, F2 cell Apply, base period Apply, combined date
Apply, lunch check, restored base/date timetable cells and lunch Header after restart,
restored base/date schedule fields, both import previews, current-period Header/highlight,
horizontal resize and overall acceptance. Codex verified the relevant persisted values,
file preservation across close/restart and owned-process normal closure. Evidence uses
normal production views/dialogs with explicitly synthetic clock/sample seed and isolated
storage. No all-keyboard/IME/DPI/monitor/geometry/timing guarantee is claimed.

Bulk Apply/clipboard roundtrip and corrupt-load native UI were not newly exercised;
full transaction/failed-load/notice/menu behavior remains automated/object evidence.
The corrupted-file tests only used disposable TEMP data. Full Backup/Restore, legacy
migration, multi-profile/groups and semester sets remain unimplemented. No commit/push
has occurred; the final report is a separate user approval gate.

## Final validation after native acceptance

- dotnet restore, dotnet build --no-restore, dotnet test --no-build --logger
  "console;verbosity=normal": all exited 0. **585 passed, 0 failed/skipped;
  build warnings/errors 0**. Baseline 508 retained plus 77 new cases.
- Final log: `C:\Users\ADMIN\AppData\Local\Temp\persistence-native-accepted-c8058829d430445aae72e551ba6a723e.log`.
- Final source/transaction/file-preservation/test-isolation review found no outstanding
  P1/P2 finding within the documented scope. Core is unchanged. Runtime publication
  remains after successful full-document saving on every wired production user path.
  No save-on-exit, partial load, corrupt overwrite or derived-state persistence exists.
- No production source changed after the reviewed native run; only acceptance and
  verification documentation changed. Stored diagnostic inputs returned to the verified
  restart state. The owned app is closed and the temporary data is retained.
- git diff --check passed. Main and origin/main remain at
  d5fc840332e3eaabc54e9dd9ab7fe38afa09510e. No staging, commit or push was performed.
  The implementation is ready for the requested final pre-commit report and user decision.

## Week Navigation regression — 2026-09-11

ViewedWeekStart and displayed dates remain transient, with no schema/DTO/save change.
Restart initializes the containing actual week from the shared clock; saved overrides
remain available when their exact dates are browsed. New isolated TEMP integration tests
exercise this wiring and verify unchanged profile bytes/mtime through navigation/restart.
The navigation milestone's verification/native status is recorded in [Week Navigation](WEEK-NAVIGATION.md).

## Schema 2 display extension — 2026-09-11

**IMPLEMENTED — PENDING NATIVE REVIEW**. [ADR 0014](adr/0014-display-presets-and-schema-v2.md)
extends schema 1 with required profile.display and retains all previous inputs unchanged.
Historical schema-1-only/no-Settings statements above describe the previous milestone.

Explicit strict v1 DTOs load original profiles as valid writable state with Standard display
defaults. No load rewrite, degraded fallback or data loss occurs for a valid v1 profile.
The next successful save of any feature writes the complete v2 profile. v2 additionally
validates layout/preset, each logical source/family/size/weight/style and format/visibility.
Missing installed families remain valid logical identities and use local rendering fallback.
Paths/remote font references and unsupported source kinds are rejected.

SaveDisplay builds its complete candidate from the latest committed other inputs; all other
save callbacks retain committed Display, excluding uncommitted preview. The same atomic
temporary write/Flush/rename then publish boundary, writer lease, expected-byte guard and
failed-load read-only safety remain. No startup/Preview/Cancel/exit write or downgrade support.
Older v1-only apps may reject schema 2. Full details and test/native evidence:
[Display Settings](DISPLAY-SETTINGS.md).
