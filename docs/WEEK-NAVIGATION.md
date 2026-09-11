# Week Navigation + Date Header

Date: 2026-09-11. Status: **IMPLEMENTED — AUTOMATED VERIFIED / USER NATIVE UX APPROVED**.
Authority: explicit milestone request and prior documented future requirement.
[ADR 0013](adr/0013-viewed-week-and-date-header.md).
Earlier preparation/pending entries are chronological evidence and are superseded by the final approval below.
Start verified: main, HEAD == origin/main == 78c4d7df35513a75ccc9f4b6b22391b1156f3cab,
working tree clean. No reset, branch switch, remote or system-setting change.

## Behavior

The first shared clock snapshot initializes the Monday containing its actual local date,
including weekend startup. Five columns show M/d over 월–금; arrows navigate exactly ±7
days. Each date independently selects its complete timetable override or Base weekday.
A schedule-only override retains Base cell content and provenance. Today header background
and actual current-period cell highlight are separate indicators. Other weeks show neither.

Actual date/time/status/countdown/lunch follow the original shared Application Clock and
today's effective schedule. Navigation reads no clock. Midnight, Monday and source changes
update actual state without navigating. Ticks preserve five column and 35 cell objects.
Base and relevant date edits update the visible sources after durable save. Editing fixes
its Base/date target at open, even if programmatic navigation occurs while the editor is
open. Normal modal ownership remains. Bulk imports always target Base.

Viewed week is transient. No profile DTO/schema changes; schema 1 and durable inputs remain.
Saved future overrides reappear when browsed after restart, which starts at the actual current
week. Date-click/calendar/Today-button actions remain deferred. Navigation disables moves
beyond DateOnly's representable five-date weeks; no wrapping/clamping to a partial week.

## Automated evidence before native review

- dotnet restore; dotnet build --no-restore; dotnet test --no-build --logger
  "console;verbosity=normal" succeeded. **623 passed, 0 failed/skipped; warnings/errors 0**.
  Baseline 585 retained plus 38 new cases. No test removed/disabled.
- Tests cover all startup weekdays/weekend, exact ±7 and repeated navigation, month/year/leap
  boundaries, DateOnly bounds, invariant culture formatting, all five independent date sources,
  schedule-only provenance, same-week midnight and Monday jumps, actual Header invariance,
  highlight removal/restoration, fixed Base/date editor targets with identical text, immediate
  future Apply/edit/remove and Base propagation across weeks.
- Real isolated TEMP profile restart verifies saved future override on its exact column,
  current-week startup, schema 1, no viewed-week field, unchanged bytes and mtime through browsing.
- Unshown WPF objects verify two-line headers, labels/automation names, bound arrow commands,
  date/cell changes, Today background trigger, equal aligned columns, stable controls and
  geometry at widths 500/620/800 across Today changes, navigation and highlight changes.
- Existing tests adapted only where the accepted semantics supersede old assumptions:
  same-week overrides stay displayed at midnight; next Monday does not auto-navigate/highlight;
  separate date-map composition replaces ApplyEffectiveDay; header object expectations account
  for dates/arrows. Existing clock-read counts, data safety, text and geometry checks remain.
- During test development, the new header query initially included the ItemsControl template's
  surrounding Border. It now selects actual TimetableDateColumn-bound borders. This was an
  object selection correction, not a geometry tolerance relaxation.
- Tests use fake/process-local clock facts, private stores and unshown WPF objects/events.
  No native keys/pointer, clipboard, system-clock mutation or production profile changes.
  Object command/layout evidence is not actual native input/rendering or all-DPI evidence.

## Self-audit

Source and regression review found no outstanding P1/P2 finding in this scope.
Viewed state has no clock/persistence dependency; MainWindow has no date calculations.
Five DateOnly map lookups retain exact identities and typed provenance; no display text or
weekday string selects an override. Timetable components and period schedules remain independent.
Actual status still uses one clock read and one actual effective-day resolution per cycle.
No navigation save, new timer, schema bump, Base import retargeting or global AppState exists.
Every existing durable callback remains after validation and before runtime publication.
Stable body controls and background-only Today/current triggers retain geometry semantics.

The chosen layout adds a narrow trailing arrow slot and uses the existing period-header
corner for Previous, with all five headers aligned to equal body columns. Exact spacing,
readability, native input and resize remain user-review candidates.

## Native checkpoint

Pending: direct launch of the normal Debug executable, outside the sandbox using the
previously user-visible launch route, with existing --effective-preview plus --bulk-preview
and a unique --dev-profile-directory under TEMP. No production flags/layout alterations.
The explicit diagnostic clock is Monday 2026-09-07 13:10 plus elapsed time, not actual PC time.
Seed sample Base schedule puts period 5 at 13:00–13:50, with distinguishable complete
2026-09-10 and 2026-09-17 timetable exceptions. Schema stays 1 and original user data is untouched.

Confirm actual desktop visibility first, then date/weekday readability, arrow placement and
week navigation, exact dated exceptions, highlight disappearance/restoration, unchanged actual
Header, Base/date edit labels, resize and restart returning to the current diagnostic week.
Codex verifies owned process startup/shutdown and profile preservation; user reports native
visual/input acceptance. No native UX acceptance, commit or push is claimed yet.

### Direct launch — awaiting user visibility confirmation

Codex launched the normal executable outside the sandbox on 2026-09-11 at
10:26:14.3281086+09:00 with the existing preview switches and isolated seeded profile.
Owned PID 55340, input-idle true, running, window handle 1181190, expected preview title;
stderr empty. Process/window existence does not establish actual desktop visibility.
No native input or clipboard action was injected. Normal window flags/layout are unchanged.

Diagnostics/profile: `C:\Users\ADMIN\AppData\Local\Temp\week-navigation-native-506f4a582bd949169ab815585062a47b`.
Profile hash at launch: `2F6059C61D643F2C14DB7FA517A79D4D680B47C901BE00A81AA8150A8666BCF3`.
The app uses only that TEMP profile. Actual visibility, native review, user UX approval,
normal shutdown/restart evidence and subsequent final validation/commit/push remain pending.
Automated log: `C:\Users\ADMIN\AppData\Local\Temp\week-navigation-tests.log` (623 passed).
`git diff --check` passed before launch; HEAD/origin/main remain at the start commit.

### User confirms actual desktop visibility

The user answered `보임` after the directly launched native window visibility question.
This establishes actual desktop visibility for this launch route. Date/header readability,
arrow UX, navigation/effective columns, highlight/status, editing labels, resize and restart
acceptance remain pending. No additional native input was injected by Codex.

### User confirms next-week behavior; authorizes direct native testing

The user answered `정상` to the next-week check: 9/14–9/18 dates, 9/17 Thursday period-1
`다음 주 예외`, no Today/current indicators, and actual Header date still 2026-09-07.
They then explicitly requested that Codex check native automation capability and execute
the remaining tests directly if possible. This authorized the foreground input interval.

### Codex native automation — passed, awaiting subjective UX approval

Computer-use 26.903.71938 with the documented node_repl @oai/sky API successfully discovered
the existing production WPF window (id 1181190), captured it and performed real native input.
No fabricated handles, changed production flags, UIA-only object test substitution or
background/noninterference claim was used. All input targeted the isolated diagnostic app.

Observed native results:

- Previous restored 9/7–9/11, the blue Monday Today header and yellow Monday period-5 cell;
  Thursday period-1 showed `이번 주 예외`. Next restored 9/14–9/18 and `다음 주 예외` on
  9/17 Thursday, with no Today/current indicators. Header remained actual diagnostic Monday
  2026-09-07, live 13:15–13:17, InPeriod(5) and corresponding countdown during browsing.
- Double-click on 9/17 Thursday period-1 opened the normal owned editor showing
  `편집 대상: 2026년 09월 17일 시간표` and Thursday period 1. Cancel closed without changes.
- Click on future-week Tuesday period-1 then real F2 opened `편집 대상: 기본 시간표`,
  Tuesday period 1. Cancel again preserved content. This verifies native entry/target labels,
  not a new native typing/IME or Apply transaction test; those retain existing evidence.
- Native system-menu keyboard size mode changed width 629→640→629, followed by Enter.
  Header/date/body alignment remained stable. Native maximize to 1920×1032 and Restore
  returned to 629×593 with aligned columns and readable date/subject/class text.
- Native title-bar X closed the app while viewing the future week. Codex verified PID 55340
  absent afterward (no force termination); stderr empty and profile bytes unchanged.
- Direct restart reused the same isolated profile and preview arguments. Owned PID 56168,
  start 2026-09-11T10:33:59.3084604+09:00, window id 327868. Fresh supported discovery and
  screenshot showed 9/7–9/11, restored Today/current indicators and saved 9/10 exception.
  Next displayed saved 9/17 exception; current indicators disappeared. Returning restored them.
- Additional Previous from current week crossed to 8/31–9/4 with Base Thursday content and
  zero Today/current indicators; Next restored 9/7–9/11. App is left in that current week
  for the user's subjective readability/arrow-layout acceptance.

Tool limitations encountered and recovered:

- The owned editor was not a separate list_windows result, but parent get_window_state
  included its screenshot and accessibility subtree. An element-index Cancel reported
  `element 167 is not available in cached app state`. After fresh discovery/capture,
  coordinate input using the returned owned-dialog screenshot id successfully clicked Cancel.
- Dragging a resize endpoint outside the current window bounds was rejected by the tool
  before input. Native keyboard sizing and maximize/restore provided actual resize evidence.
  No freehand outside-window resize drag or all-DPI/monitor guarantee is claimed.

The post-restart process identity was verified and is still running, restart stderr empty.
Profile SHA-256 remains `2F6059C61D643F2C14DB7FA517A79D4D680B47C901BE00A81AA8150A8666BCF3`,
identical to the initial seed. Diagnostic time is synthetic/process-local and explicitly
labeled in the window; Windows clock and real user profile were not changed. Screenshots
were inspected as native captures, not treated as evidence of human UX preference.

No implementation code changed during native testing. Automated baseline remains 623
passed / build warning-error 0. No P1/P2 finding in observed native scope. Overall user
UX approval is still pending; no commit/push until that approval and final reverification.

## User native UX approval — 2026-09-11

The user explicitly stated `Native UX 승인합니다.` and confirmed date/weekday readability,
understandable arrow placement, week navigation, per-date override display, current highlight
clearing/restoration, actual-clock Header, F2/double-click target labels, resize/maximize/restore,
current-week restart, retained date overrides and no unnecessary profile modification.

The user explicitly defers detailed arrow/header visual polish to a future styling/display
milestone and does not consider it a blocker. No additional implementation or scope expansion
is authorized for this completion step. This supersedes preceding pending acceptance entries.
Final build/test, self-audit, diff check, commit and normal fast-forward origin/main push are
explicitly authorized. No force push, new branch or remote/credential/system-setting change.

The reviewed implementation is unchanged. After acceptance Codex verified the restarted
process by PID 56168, executable path and exact start time, requested CloseMainWindow and
observed normal exit within ten seconds. This cleanup is programmatic normal close, distinct
from the earlier native title-bar X test. No force termination; ExitCode was unavailable, so
exit-code-zero is not asserted. Final profile hash and stderr are checked with final validation.

## Final validation after native UX approval

- dotnet restore, dotnet build --no-restore and dotnet test --no-build --logger
  "console;verbosity=normal" all exited 0. **623 passed, 0 failed/skipped; build warnings/errors 0**.
  Baseline 585 plus 38 new cases; no extra implementation or scope extension after acceptance.
- Final log: `C:\Users\ADMIN\AppData\Local\Temp\week-navigation-final-b9f281675a40443295ff3acfcac82446.log`.
- Final source/diff self-audit: no outstanding P1/P2 finding. Confirmed Monday-derived transient
  week state, exact-date projections/provenance, one actual snapshot/status pipeline, no
  navigation persistence/schema change, captured editor targets, Base-only import, independent
  schedule semantics and background-only Today/current styling. MainWindow contains no new
  date logic and no new clock or global state was added.
- Native diagnostics are closed: PID 56168 absent, restart stderr length 0, final profile
  SHA-256 `2F6059C61D643F2C14DB7FA517A79D4D680B47C901BE00A81AA8150A8666BCF3`, unchanged
  from the seed and through both normal closes. TEMP diagnostics retained for reference.
- git diff --check passed. All changed source/tests are the native-reviewed implementation;
  only documentation acceptance/evidence changed afterward. Final commit/push identity and
  remote synchronization are reported in the completion response rather than embedded as a
  self-referential commit hash in this document.

## Styling follow-up — 2026-09-11

The deferred arrow/header polish is now **IMPLEMENTED — PENDING NATIVE REVIEW** in
[Display Settings](DISPLAY-SETTINGS.md). ‹/› have local hover/pressed/focus styles;
M/d is visually stronger than weekday and Today has a softer background. Five-column,
35-cell, date-provenance, navigation, transient viewed week and highlight geometry
contracts are unchanged. Header display settings do not alter column date formatting.
Schema 2 is introduced solely for display inputs (ADR 0014), never for viewed week.
The previous milestone's native approval does not approve these new visual candidates.
