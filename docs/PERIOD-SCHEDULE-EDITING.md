# Period Schedule Editing Foundation

Status: **IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE SMOKE PASSED (limited scope recorded below)**.
Date: 2026-09-10. Authority: user's milestone request and explicit chronological
ordering confirmation; [ADR 0009](adr/0009-editable-base-period-schedule.md).

## Implemented behavior

The timetable context menu opens `일과 시간 편집...`. Seven read-only period numbers
have separate start/end text inputs in exact 24-hour HH:mm. Draft can be empty or
invalid. No runtime mutation occurs during editing. Apply parses/validates the entire
candidate; the first invalid field or adjacent period pair gets a Korean explanation
and the dialog stays open. Correction and repeated Apply are possible.

A valid base schedule has exactly ordered 1..7, Start < End and previous End <= next
Start. Touching/gaps allowed; overlap/reversed time order rejected, no sort or renumber.
Core's general unordered resolver behavior is preserved. Runtime replaces one complete
immutable reference. An explicit application callback immediately refreshes shared
status/countdown/date/time/highlight. Cancel/X/Escape discard. Apply closes on success.
No persistence: restart uses defaults. No timetable cell content or geometry mutation.

## Self-audit

| Check | Result |
| --- | --- |
| Row-by-row runtime mutation / invalid partial Apply | None; strings remain Draft, only a complete validated value reaches replacement |
| Old schedule stuck in production refresh loop | App injects the live runtime accessor; fixed-input overload is only for non-editable callers |
| Multiple clock/schedule reads per cycle | One of each; same snapshots feed the complete calculation and publication |
| Editor owns resolver or clock | Session/window do not; owner adapter calls App-supplied refresh callback |
| Core WPF / localized errors | None added; immutable schedule invariants only |
| Coupling to timetable SubjectText/ClassText | None; existing week and stable cells preserved |
| Silent reorder / period renumber | None; invalid order rejects |
| Persistence / Date Override / Font manager | Not implemented |
| Global state / generic repository / event bus / DI | Not introduced |

Expected validation/target-stale failures occur before runtime mutation. The owner
and loop operate on the UI dispatcher, with no await or nested message pump between
replacement and immediate refresh. This is not cross-thread or durable storage
transactionality. Unexpected application/refresh exceptions are not reported as a
successful Apply. Automated evidence does not imply native keyboard/IME/X behavior.

## Native checkpoint plan

Codex first launches the normal executable directly with `--period-preview --bulk-preview`.
This labels process-local synthetic Monday 2026-09-07 13:10+ time and sample data;
production window flags/editor/layout remain unchanged. No clipboard overwrite or
system clock mutation. `--bulk-preview` only preloads import samples; the user can
explicitly read/copy clipboard through existing actions. Actual visibility requires
user confirmation; PID/window handle alone is not visibility evidence.

Confirm date readability, context entry/seven rows, field and order errors, Cancel/X,
then set period 5 to 13:00–13:50 and Apply: Break → period 5 and Monday 5 highlight
must change together immediately, with stable timetable geometry. Check existing
cell editing and School/Canonical import. Restart the same development mode to verify
default 14:00–14:50 returns. Process ownership and shutdown are checked by Codex.
Only actual observed results will be recorded as passed. Native approval is required
before final restore/build/test/self-audit and authorized commit/fast-forward push.

## Automated verification — before native review

- Baseline 413 retained; 45 new period Core/session/refresh/WPF cases added.
- `dotnet restore SchoolTimetableWidget.sln`, `dotnet build SchoolTimetableWidget.sln
  --no-restore`, `dotnet test SchoolTimetableWidget.sln --no-build --logger
  "console;verbosity=normal"` all exit 0. Build warnings/errors 0; **458 passed,
  failed/skipped 0**. Log:
  `C:\Users\ADMIN\AppData\Local\Temp\period-automated-644b10d0ab164a03a649b2c1655e38bc.log`.
- First regression run found one existing import test hard-coded to three menu
  commands; the approved fourth schedule entry was added to its expectations.
  Existing clipboard/paste/import assertions remain; no tests disabled or skipped.
- Tests cover immutable complete 7, bad intervals/count/identity/order, gap/touching,
  stale/atomic replacement, padded Drafts and exact parsing, one bad field rejecting
  all edits, repeated invalid Apply/correction, Cancel/programmatic Close, immediate
  Break→5 transition, one clock/source read, all state/boundary cases, separate
  timetable content and compiled WPF inputs/errors/menu/stable cell rectangles.
- Source audit: direct PC read remains only the existing fallback adapter. Production
  refresh has one `_clock.GetSnapshot()` and one `_getSchedule()` call. New Core
  schedule has no Desktop/WPF/Toolkit/localized validation dependency. No generic
  state, persistence, override or font implementation added. No P1/P2 finding remains
  in the automated/source scope. `git diff --check` passed.
- WPF evidence is unshown object/binding/routed-event/Measure/Arrange only. Native
  visibility, readability, actual keyboard/X/clipboard gestures and user acceptance
  remain pending. No commit/push before that approval.

## Direct native launch — awaiting user visibility

Codex launched the built executable directly outside the sandbox using
`--period-preview --bulk-preview`, normal window style, on 2026-09-10.
Owned PID **90088**, start time **2026-09-10T14:23:37.9735997+09:00**;
input-idle true, running, SessionId 1, window handle 3608050 and expected
`일과 편집 검증 · 월요일 13:10부터 모의 시각` title observed.
Process identity/log directory:
`C:\Users\ADMIN\AppData\Local\Temp\period-native-223cf589205242298137247e89130b64`.
This establishes launch/window existence, not actual user visibility or UX approval.
No pointer/key injection, clipboard write or system clock change was performed.
Awaiting the user's first visibility/readability checkpoint; commit/push remain pending.

### User screenshot confirms native visibility

The user supplied a screenshot of the directly launched development window:
`C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-047e6552-cb99-4c21-836a-db113b91592f.png`.
It shows the expected synthetic-time title, `2026년 09월 07일`, `13:10:25`,
`쉬는시간 · 5교시까지 49분`, Monday–Friday/seven rows and sample timetable content.
This confirms actual desktop visibility and the separate date/time/status display
at the captured size. No obvious Header overlap/clipping is visible in this image.
Subjective readability/UX approval, editing, invalid/Cancel/X/Apply, immediate
highlight change and restart checks are still pending; this image does not prove them.

### User confirms editor entry and rows

The user answered `ㅇ` to opening timetable context menu → `일과 시간 편집...`
and checking that start/end inputs for periods 1–7 are visible. This records native
entry and seven-row visibility, not yet validation, Apply or cancellation behavior.

### User confirms invalid-time error

After the instruction to change period 3 End to `25:99` and click Apply, the user
answered `표시됨` to the expected period-3-end validation error. This records native
error visibility. Exact no-partial-mutation guarantees are additionally established
by automated tests; cancellation and successful Apply remain to be checked.

### User confirms Cancel discards invalid Draft

The user answered `ㅇ` after Cancel → reopen `일과 시간 편집...` and checking that
period 3 End is the original `11:50`. This confirms native Cancel/reopen preserved
that original field after the rejected invalid Apply. Other fields' all-or-nothing
behavior remains covered by automated tests.

### User confirms native X discards Draft

The user changed period 5 Start to `13:00`, closed the editor with X without Apply,
and answered `유지` to reopening and checking the original `14:00`. This records
user-reported native X/cancel semantics for the period editor.

### User confirms highlight after successful Apply workflow

After setting period 5 to `13:00`–`13:50` and clicking Apply, the user answered
`강조됨`. This explicitly confirms the requested current-cell highlight appeared.
The response does not separately establish the exact Header text, update latency,
or dialog-close observation; those are not inferred as individually measured results.
Immediate shared refresh and Apply-and-close remain covered by automated tests.

### User confirms Header readability and horizontal resize stability

The user answered `ㅇ` after narrowing/widening the window and checking readable
Date/Time/`5교시 · 종료까지 …` text and stable timetable layout. This records limited
native Header/current-status readability and resize acceptance at the user's tested
sizes. It is not pixel measurement, all-DPI verification or exact refresh latency.

### User confirms cell editing after schedule Apply

The user answered `ㅇ` after selecting a cell, using F2, changing Subject or Class,
and applying, confirming that the edited content displays normally. This records
limited native cell-editing regression evidence following schedule replacement.
Exact neighbor/value preservation and double-click routing remain automated evidence.

### User confirms School and Canonical preview regression

The user answered `미리보기 정상` after instructions to open School and Canonical
imports in turn, select a sample teacher for School, inspect both previews and cancel.
This confirms both native preview displays. No new native clipboard roundtrip or
whole-week import Apply is inferred; those existing behaviors retain automated
regression coverage and the prior Bulk milestone's separately recorded native evidence.

### Owned normal shutdown and restart

Codex verified PID 90088's exact executable path and saved start time, requested
CloseMainWindow and observed exit within ten seconds. No force termination.
ExitCode was unavailable/null; no exit-code-zero or physical title-bar click claim.

Codex directly restarted the same executable and development arguments outside the
sandbox. New owned PID **97500**, start time **2026-09-10T14:28:46.0237644+09:00**,
input-idle true, running, expected title and handle 1641974. New identity/log directory:
`C:\Users\ADMIN\AppData\Local\Temp\period-native-restart-748d53a45071454a9f218cdcf3672187`.
User confirmation of default schedule restoration and final native UX acceptance
is pending; process observations alone do not establish those outcomes.

### User confirms restart defaults and grants native approval

The user answered **`native 승인`** after the final request to verify period 5 was
restored to `14:00`–`14:50` after restart and approve the reviewed UX if no changes
were needed. This records user acceptance of restart-to-default behavior and the
limited native milestone review. No source/UI adjustment was requested.

Native evidence comprises the visibility screenshot and the explicitly recorded
user observations above. It is not exhaustive native keyboard/IME, import clipboard
roundtrip/Apply, all boundary timing, all monitor/DPI or pixel measurement evidence.
Those limits remain separate from automated contract/object coverage.

The user's required native gate is satisfied. Final restore/build/test, self-audit,
diff check and the authorized commit/normal fast-forward push remain to be performed.

### Accepted-run normal shutdown

After native approval, Codex verified PID 97500's saved executable path and exact
start time, requested CloseMainWindow and observed exit within ten seconds and
remaining owned PID count 0. Stderr length 0. No force termination. ExitCode was
unavailable/null, so exit-code-zero is not claimed. This is normal process-close
evidence, distinct from the user's earlier physical editor-X cancellation check.

## Final verification after native acceptance

- Restore, build --no-restore and test --no-build --logger console;verbosity=normal
  rerun after explicit native approval and owned-process shutdown: all exit 0.
  **458 passed, failed/skipped 0; build warnings/errors 0**. Log:
  `C:\Users\ADMIN\AppData\Local\Temp\period-accepted-d8257c0dbc824bbdb0c133eeee50c629.log`.
- Final self-audit rechecked immutable complete chronological base schedules, no
  row-by-row runtime mutation, reject-before-change validation/stale handling,
  one clock/one schedule snapshot, immediate shared refresh, unchanged timetable
  contents, and Core/Desktop separation. No outstanding P1/P2 finding in this scope.
- No production source changed after native approval; only evidence/status documents
  were updated. Persistence, Date Override and clock/font Settings remain PLANNED.
- Tracked diff and new-file whitespace checks passed. Native evidence and limitations
  remain recorded separately. Required implementation/automated/native gates are met;
  the user authorized committing this milestone and a normal fast-forward push to
  `chuthulhu/school-timetable-widget-next` origin/main. No force/configuration changes.
