# Effective Day / Date Overrides / Optional Lunch

Status: **IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE REVIEW ACCEPTED (limited scope below)**.
Date: 2026-09-10. Authority: milestone request and explicit user confirmation of
seven-cell snapshots, displayed provenance and fixed open-editor targets.
[ADR 0011](adr/0011-effective-day-and-date-overrides.md), [ADR 0010](adr/0010-optional-lunch-break-presentation.md).
Start state: main, HEAD == origin/main == c27d0c8, working tree clean.
Preparation/pending entries below are chronological history; the final acceptance
and verification sections supersede their pending statuses.

## Behavior and boundaries

DateOnly keys select an independent complete seven-cell timetable and/or complete
seven-period chronological schedule. Base data is preserved. The effective 35-cell
grid replaces only today's weekday, while one clock snapshot and one effective
resolution drive status/countdown/date/time/highlight/lunch. A schedule-only entry
leaves every timetable cell's provenance at Base.

F2/double-click edits the displayed source, with explicit Base/date label and a
captured source/date/weekday/period. Midnight never retargets an open editor. Removed
or replaced timetable targets reject before mutation. Editing another component
cannot overwrite a current schedule. Bulk import is explicitly Base-only and keeps
all date snapshots; removing an override reveals the latest Base values.

Context menu → 날짜별 예외 설정 → choose date → 이 날짜 편집. Date remains locked
while editing. 다른 날짜 선택 (초안 폐기) explicitly discards that Draft. Each toggle
controls one component. Disabled components fall back to Base after Apply; 전체 예외
해제 clears both toggles and still requires Apply. Cancel/X/Escape preserve the prior
runtime entry. All enabled components validate before one entry replacement. Invalid
Apply retains the Draft/error; successful Apply closes. Weekends are rejected.

Lunch menu defaults OFF; ON renames only Break in the effective period-4-end to
period-5-start interval. Touching has no interval. Countdown is unchanged, lunch has
zero current cells, and Core still has exactly five states. CurrentDateText/time/
status remain separate. Overrides and lunch option last only until exit.

## Automated evidence

Final pre-native restore → build --no-restore → test --no-build --logger
"console;verbosity=normal" all exited 0. **508 passed, 0 failed/skipped; 0 build
warnings/errors**. Baseline 458 retained plus 50 new cases. No tests disabled.
Log: `C:\Users\ADMIN\AppData\Local\Temp\effective-day-automated-8c3171d58ab74535bf9b1f774a8d4a9c.log`.

Coverage: four independent resolution combinations; seven-slot/null/immutability/date
validation; weekend rejection; per-component/full removal and other-date isolation;
base-seeded Drafts and exact strings; invalid either component rejects both, correction,
reopen-after-success failure retention, stale/Cancel/Close; immediate shared refresh,
future edit no-refresh, next-day Base fallback, midnight one-read sequence; provenance
for equal text and schedule-only overrides; fixed-date editor across midnight; deleted
or replaced target rejection and schedule preservation; Base-only import coexistence;
lunch boundaries, other gaps, shifted effective 4/5 endpoints and touching; default-OFF
restart state; WPF DatePicker/toggles/Apply/Cancel/remove/error/menu binding, date target
label, stable controls and highlight/lunch geometry. Existing editor/import/date/period
regressions all pass. Main native rendering and physical input are still pending.
Tests use immutable Core values, fake clocks, private runtime instances, and unshown
WPF objects/bindings/routed events/Measure/Arrange. They do not use native key/pointer
input, clipboard, OS dialogs, original settings or system-clock changes. Programmatic
Close is not native title-bar X; object layout is not proof of actual desktop rendering.

## Self-audit

- Base timetable and schedule remain separate immutable snapshots; no destructive merge.
- Timetable and schedule components can be independently created/removed; null/null has no stored entry.
- Day snapshot is exactly seven complete pairs, with exact empty/whitespace/newline preservation.
- Runtime map is private and instance-owned. Day values are reusable under future teacher profiles;
  schedules remain independent school-day values. No global AppState or generic repository.
- One production GetSnapshot and one effective resolver call per refresh; no clock reads in
  editors/formatters/Core resolver. No system clock mutation or new timer.
- Same captured effective schedule feeds status/countdown/highlight/lunch and same result feeds grid.
- All validation precedes date replacement. Stale/Cancel/rejected Apply cannot leave half an entry.
- Source routing uses displayed override component + weekday, never content equality or schedule presence.
- Open editor closes over its initial date/source/slot and cannot be retargeted by refresh.
- Weekend remains authoritative. No Lunch kind or hard-coded lunch time; non-4→5 gaps stay Break label.
- Existing 35-cell controls and styles remain; content minimum updates on changed text only.
- Persistence, multi-profile/groups, semester sets, font manager and Settings are not implemented.

Final source/diff self-audit found no outstanding P1/P2 issue within this scope.
The private store uses one complete entry swap; one production clock read and one
composition resolver invocation were rechecked. Core remains platform/string-free.
No persistence/profile/font/OS setting changes were added. `git diff --check` passes;
new files were additionally checked for trailing whitespace. Native UX acceptance
remains the required next checkpoint.

## Native checkpoint plan

Directly launch the normal executable outside the sandbox using the previously
user-visible launch path with --effective-preview --bulk-preview. This opt-in mode
uses existing process-local Monday 2026-09-07 13:10 plus elapsed time and anonymous
sample timetable/import content. It starts with NO overrides and lunch OFF. Original
window flags and production dialogs/layout remain unchanged. No system clock or
clipboard mutation occurs at launch. User-requested import clipboard actions remain explicit.

First confirm actual visibility; PID/handle/input-idle do not establish it. Then review
entry/date selection, timetable-only/schedule-only/both, per-component/full removal,
explicit F2 target labels, instant Header/Highlight after period-5 13:00–13:50 override,
lunch OFF/ON in the 4→5 gap, other-gap label, geometry, existing F2/import/base period
editor regression, and fresh-process reset. Only user-observed checks become native
evidence; exhaustive boundaries remain automatic evidence. No commit/push before native approval.

## Direct native launch — awaiting visibility

Codex launched the built executable outside the sandbox with --effective-preview
--bulk-preview and normal window style, reusing the previously user-visible route.
Owned PID **99700**, start **2026-09-10T14:57:19.4788146+09:00**, input-idle true,
running, handle 5770502 and the expected date-preview title were observed.
Process identity/stdout/stderr: `C:\Users\ADMIN\AppData\Local\Temp\effective-day-native-d49cc8adda7146fd964aecb58ff2001e`.
This establishes process/window existence only. Actual visibility, readability and
native user acceptance are pending. No pointer/key input or clipboard write was used.
Implementation remains uncommitted; native approval is required before final
reverification, commit and authorized normal fast-forward push.

### User screenshot confirms actual desktop visibility

The user supplied `C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-3e539172-5b4e-4940-b272-a997ef62261a.png`.
It shows the directly launched date-preview title, `2026년 09월 07일`, `13:10:44`,
`쉬는시간 · 5교시까지 49분`, all five weekday columns and seven period rows.
No current-cell highlight is visible, consistent with Break. The independent
Header fields show no obvious overlap at the captured size. This confirms actual
window visibility and that captured presentation only; subjective readability,
editor interaction, lunch toggling, restart and overall native UX approval remain pending.

### User confirms date editor entry

The user answered `보임` after opening the timetable context menu's 날짜별 예외 설정
entry and checking the date selector and 이 날짜 편집 button. This records native
entry/control visibility only; date selection, Draft editing and Apply remain pending.

### User confirms timetable-only override Apply

The user answered `바뀜` after selecting 2026-09-07, enabling only the timetable
component, changing period 1 Subject to 예외 수업 and applying. This records the
reported Monday-period-1 display change through the timetable-only workflow.
Unchanged base values and other slots are established by automated tests, not
inferred as separately observed native results. F2 provenance labeling remains next.

### User confirms date-specific F2 target label

The user answered `표시됨` after selecting the overridden Monday-period-1 cell,
pressing F2 and checking 편집 대상: 2026년 09월 07일 시간표. This records native
F2 entry and the explicit date target label. Fixed targeting across midnight and
text-equality independence remain automated evidence.

### User screenshot confirms both overrides, Header and highlight

The user supplied `C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-95016dcb-4daa-4edb-b854-966164842da7.png`
after retaining the timetable component and applying period 5 as 13:00–13:50.
The image shows 2026년 09월 07일, 13:20:59, 5교시 · 종료까지 29분,
Monday-period-5 highlighted, and Monday-period-1 still displaying 예외 수업.
This is native evidence that timetable and schedule overrides coexist and the
captured Header/current highlight agree. It does not measure update latency or
establish all-slot/pixel invariance; those remain automated/limited evidence.

### User screenshot confirms timetable removal with schedule-only override retained

The user supplied `C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-07414109-3ee6-4a1a-a6b3-80327a905ffe.png`
after disabling only the timetable component and applying. It shows Monday-period-1
restored to 국어, while 13:21:38, 5교시 · 종료까지 28분 and the Monday-period-5
highlight remain. This confirms the observed timetable fallback and independent
schedule-only runtime presentation. Schedule-only F2 Base labeling is the next check.

### User confirms Base F2 target with schedule-only override

The user answered `펴시됨` (understood as 표시됨) after opening Monday-period-1
with F2 and checking 편집 대상: 기본 시간표 while only the date schedule override
remained. This records native Base target labeling for the schedule-only case;
schedule presence did not route timetable editing to a date override.

### User screenshot confirms full removal and base schedule fallback

The user supplied `C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-cd2ead4d-694f-43fc-bc7c-3c6dd7f6060a.png`
after 전체 예외 해제 followed by Apply. The image shows 2026년 09월 07일,
13:30:54, 쉬는시간 · 5교시까지 29분, the base timetable content and no yellow
current-period highlight. Monday-period-1 retains a dotted selection/focus outline,
which is distinct from the clock-driven current highlight. This confirms observed
full-removal fallback; lunch toggle behavior remains the next native check.

### User confirms lunch option ON in the effective 4-to-5 gap

The user answered `ㅇ` after enabling the context-menu lunch option and checking
that the Header changed to 점심시간 · 5교시까지 … while no yellow current-cell
highlight appeared. Together with the preceding OFF screenshot, this records
native OFF/ON label behavior in the base effective 4-to-5 gap. Other-gap behavior
and restart-to-OFF remain pending native checks.

### User screenshot confirms non-lunch gap uses the effective overridden schedule

The user supplied `C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-d1e2e963-9cc9-472f-bcb8-16f54e5284d9.png`
after leaving lunch ON and applying a schedule-only override with period 5 at
13:00–13:05. The image shows 13:37:00, 쉬는시간 · 6교시까지 1시간 22분 and no
yellow current-cell highlight. This records native ordinary-Break labeling in the
effective 5-to-6 gap despite lunch being enabled by the preceding workflow.
The screenshot does not itself display the menu check state; that state is supported
by the prior user confirmation and instruction to leave it enabled. Other gap and
exact-boundary combinations retain automated coverage.

### User confirms existing School and Canonical import previews

The user answered `ㅇ` after being asked to open both School and Canonical import
previews and cancel. This records user-reported normal preview display in both
existing import modes while a date schedule override remains active. It does not
establish a new native clipboard roundtrip or import Apply; Base-only replacement
and date-override preservation remain covered by automated tests.

### User confirms base period editor and resize regression checks

The user answered `정상` after checking that the base period editor still shows
period 5 as 14:00–14:50, canceling, and narrowing/widening the main window while
checking date/time/status/timetable presentation. This records limited native
base-schedule preservation/editor access and user-reported resize/display stability.
It is not all-DPI/monitor or pixel measurement evidence. Fresh-process reset and
explicit overall native acceptance are the remaining native gate.

### Owned normal shutdown and fresh-process launch

Codex verified owned PID 99700 against its saved executable path and exact start
time, requested CloseMainWindow and observed exit within ten seconds with remaining
owned PID count 0. Stderr length was 0. No force termination; this is process-close
evidence, not a physical title-bar X gesture or an observed exit-code-zero claim.

The same executable/arguments were directly restarted outside the sandbox. New
owned PID **116912**, start **2026-09-10T15:36:11.8593953+09:00**, input-idle true,
running, expected date-preview title, handle 5180794. Identity/log directory:
`C:\Users\ADMIN\AppData\Local\Temp\effective-day-native-restart-b476457692624873aa8de37884fa42e9`.
Actual fresh-window visibility, schedule override reset and lunch-OFF presentation
await user confirmation. The prior timetable override was explicitly removed before
shutdown, so native restart evidence will not independently prove removal of a
populated timetable component; private-store fresh-instance tests cover that case.
Overall native approval is still pending. No commit/push performed.

### User accepts the final restart/native checkpoint

The user answered **`정상`** to the final checkpoint asking them to verify the
restarted Header returned to 쉬는시간 · 5교시까지 …, the lunch menu was unchecked,
and accept the reviewed UX if no changes were needed. This response is treated as
acceptance of that final native checkpoint; it is not quoted as the literal phrase
native 승인. No UX adjustment was requested. The initial milestone authorization
permits final verification, commit and a normal fast-forward push after this gate.

This records reported schedule-override reset and lunch-OFF behavior on fresh launch.
The populated timetable component had already been removed before restart; its
fresh-store behavior remains automated evidence. Native scope comprises the listed
screenshots and user observations: date entry, timetable-only/both/schedule-only,
component/full removal, both F2 target labels, Header/highlight agreement, lunch
4-to-5 and non-lunch 5-to-6 labels, import previews, base period editor, limited
resize and fresh-process reset. It is not exhaustive native IME/keyboard, midnight,
all date-picker interactions, invalid/Cancel/X combinations, clipboard import Apply,
all DPI/monitors or pixel measurement. Those boundaries retain their automated or
prior native evidence rather than being newly claimed here.

### Accepted-run normal shutdown

Codex verified PID 116912's exact executable path and saved start time, requested
CloseMainWindow and observed exit within ten seconds with remaining owned PID count 0.
Stderr length 0. No force termination, physical-X or exit-code-zero claim.
Final restore/build/test, source/diff audit and publication follow this acceptance.

## Final verification after native acceptance

- After the final user checkpoint and owned-process shutdown, `dotnet restore`,
  `dotnet build --no-restore`, and `dotnet test --no-build --logger
  "console;verbosity=normal"` all exited 0. **508 passed, 0 failed/skipped;
  0 build warnings/errors**. Baseline 458 + 50 new cases; no tests disabled.
- Final log: `C:\Users\ADMIN\AppData\Local\Temp\effective-day-accepted-d270c0c678ac455f802d67d7e3c9125f.log`.
- Final source/diff audit rechecked base immutability, independent components,
  complete seven-cell snapshot, one clock/effective result, midnight targeting,
  provenance routing, validate-before-replace, weekend priority and effective 4/5
  lunch rules. No outstanding P1/P2 finding in the implemented scope.
- No production code changed after the accepted native run. Only native evidence,
  current status and final verification documentation changed. A broad status-text
  replacement was narrowed to preserve a prior milestone's historical pending entry.
- Persistence, profiles/groups, semester sets, date imports, font management and
  durable Settings remain unimplemented. Restart behavior is intentionally volatile.
- Tracked diff and new-file whitespace checks passed. The reviewed milestone is
  ready for its authorized main commit and normal fast-forward origin/main push.
  Git author configuration is absent; the consistent author from the three latest
  main commits is used through command-local -c options only, without configuration changes.

## Week Navigation follow-up — 2026-09-11

The previous single-today grid behavior is superseded by [ADR 0013](adr/0013-viewed-week-and-date-header.md).
Each displayed date now resolves its own complete timetable override. Date Apply/removal
refreshes the visible week even when editing a future/past date, while only actual-today
changes request status recalculation. This historical milestone's native observations do
not establish native acceptance of the new navigation UI. See [Week Navigation](WEEK-NAVIGATION.md).
