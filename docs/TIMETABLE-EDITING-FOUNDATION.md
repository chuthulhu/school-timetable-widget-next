# Timetable Editing Foundation

Date: 2026-09-10

Scope authorized by the user: Golden Reference investigation, editing contract,
one-cell editing among 35 independent cells, Draft/commit/cancel, lossless text,
highlight coexistence, automated verification, self-audit and user native check.
Persistence is explicitly excluded. Restart/load/save is a later milestone.

## Golden Reference investigation

Read-only local Git inspection at fixed commit
`84de32a555633120bd6363a609a19cbc0a15e8ea` in
`chuthulhu/school-timetable-widget`:

- `docs/TIMETABLE-BEHAVIOR-SPEC.md`: A = automated Qt object/TEMP evidence,
  N = earlier user native evidence, S = source inspection. These evidence grades
  describe Legacy, not this WPF implementation.
- `src/gui/dialogs/timetable_dialog.py`: source inspected in this milestone.
  A separate 7×5 table copies current values. A plain-text multiline delegate
  exchanges text with the table. Save collects values, updates the manager and
  main view, and accepts/closes. Cancel rejects. No separate Apply button.
- Legacy documents record double-click/F2 entry, Enter newline, native Korean
  two-line input, Save/reopen/restart, Cancel and title-bar X. We did not rerun
  the Legacy app or claim new native evidence.
- Preserve the normal meaning of explicit acceptance, draft discard, empty
  values, Unicode, leading/trailing spaces, whitespace-only and multiline text.
- Exclude inferred/manual merges and their neighbouring-cell overwrite bugs
  (A2/I7), AutoText/HTML interpretation (M1/I6), and the swallowed disk-write
  failure followed by an Accepted dialog (R16/I3).
- The source repository was not modified or copied. A command-local
  `safe.directory` setting allowed read-only Git inspection; global Git settings
  were not changed.

## Final authorized scope

**COMPLETE — AUTOMATED VERIFIED / SELF-AUDITED / USER NATIVE SMOKE PASSED (limited scope below).**
The user approved double-click/F2, one-cell dialog, Draft isolation, explicit
Apply-and-close, Cancel/X, multiline and in-memory-only semantics. The subsequent
scope adjustment makes SubjectText/ClassText the canonical editable pair.
[Product Contract](PRODUCT-CONTRACT.md#timetable-editing-foundation--approved-2026-09-10)
and [ADR 0006](adr/0006-single-cell-in-memory-editing.md) record the final contract.

- Core TimetableCellValue is an immutable pair of exact non-null strings.
  TimetableCell holds slot identity + Value. WithCellValue creates one complete
  weekly snapshot with exactly one changed pair; other slots/source are preserved.
- Desktop DisplayText is a one-way plain-text projection. Exactly empty fields
  add no separator; whitespace-only remains data. No reverse parsing is possible
  or attempted. Old Content APIs/temporary single-text editor code were removed.
- CellEditSession owns two Draft fields and a target callback; no weekly singleton,
  clock, profile or date dependency. WeeklyTimetableEditor binds the captured
  weekly slot to that session. One active session, terminal Apply/Cancel, rejected
  Apply retains both Drafts/error. Stable cell VMs retain current highlight.
- Actual UI has separate 교과/반 multiline TextBoxes, 적용/취소, X/Escape cancel,
  and no default Apply or extra Apply shortcut. Target focus has an opacity-only
  dashed outline; current-period background is independent. Tab traverses inputs
  and buttons. Mouse double-click and F2 enter the same captured-cell workflow.
- Owned modal editor uses the existing dispatcher, leaving the existing shared
  header/highlight refresh loop active. Apply updates the accepted snapshot and
  both value/projection before notifying; view refreshes measured window minimum
  after closing. No per-tick content minimum work is added.
- All runtime values last for this app run. No persistence/schema, Settings,
  application undo/redo, bulk editing/import/parser, Date Override, date display,
  teacher/group models, multi-tab UI or importer/provider hierarchy is added.
- Future DateOnly overrides, same-snapshot CurrentDateText, explicit School /
  Canonical / Small Paste modes, and stable teacher profiles/group references are
  documented in ARCHITECTURE/FEATURE-MAP. These are not implemented features.

## Automated verification — 2026-09-10

Final commands: `dotnet restore`, `dotnet build --no-restore`,
`dotnet test --no-build --logger "console;verbosity=minimal"`, all exit 0.
Build: **0 warnings, 0 errors**. Tests: **331 passed, 0 failed, 0 skipped**.
Existing baseline 276 plus 55 new cases; existing fixture values were explicitly
carried into SubjectText with empty ClassText and existing assertions retained
against canonical value/DisplayText as appropriate.

| Evidence | Coverage |
| --- | --- |
| Core replacement: 10 cases | All 35 targets, no neighbour/source mutation, exact strings, invalid target/null rejection, no-op snapshot |
| Session/owner/formatter: 24 cases | All 35 target slots across ten pair fixtures; two-field atomic notification state; Apply/reopen/Cancel, one active session, stale terminal calls, rejection with Draft retention, independent owners, null fields, exact-empty display, whole legacy string mapping, identical display with distinct field boundaries |
| WPF objects/events/layout: 21 cases | Both actual bound TextBoxes through Apply/reopen, Cancel click/programmatic Close, rejected Apply bound error, multiline/tab/default-button policy, F2 event route per all 35 controls, distinct focus/highlight styles, shared-clock header/highlight during Draft and Apply, measured minimum growth/clear and stable highlight geometry |

Text fixtures include empty, whitespace-only, tabs, leading/trailing spaces,
LF/CRLF/CR and mixed newline strings, Korean, Unicode/emoji/combining characters,
literal markup and 2,000-character fields. Partial SelectedText insertion into
both bound TextBoxes also preserves existing line endings. 2,000 is a test fixture,
not a product length cap. Exact whitespace preservation is string-level evidence,
not an inference from screenshots.

The first expanded UI run had 329 passed / 1 failed because the unshown Window's
visual root had not been materialized; a test enumerated the Window instead of
its actual content root. Corrected the test to inspect the content root and added
an explicit nonempty count to its input-layout assertion. Subsequent run passed
330; the final same-display/different-field-boundaries regression brings it to 331.
This was a test inspection error, not an observed native UI defect.

All WPF evidence uses unshown objects, bindings, Measure/Arrange and synthetic
routed events on STA dispatchers. No Window.Show/ShowDialog, native keys, pointer
movement, clipboard access, user profile, system clock mutation or updater/
notification/autostart side effect was used by these tests. Programmatic Close
is not title-bar X evidence; F2 routed events are not actual keyboard evidence;
TextBox edits are not IME composition or OS focus-loss evidence.

## Self-audit

No outstanding P1/P2 finding in the inspected scope.

- Reviewed new/changed Core, editor/session/adapter, view, presentation, minimum
  helper, tests and current contracts. Pair validation happens before snapshot
  replacement; both canonical fields and display are coherent before notification.
  Draft and terminal state cannot mutate another target or roll back earlier Apply.
- No string-based identity, merge, normalization, trimming or reverse display
  parsing. Legacy source/history was not copied. Core has no WPF/Toolkit/Desktop
  dependency and no teacher/group/date/importer concept was introduced into it.
- CurrentStatusRefreshLoop, ApplicationClock implementations, MainWindow, App and
  period/header logic are unchanged. There is one clock refresh pipeline; editing
  adds neither timer nor direct PC clock reads. IsCurrent stays day/period based.
- No persistence/file/clipboard API in editing. No app-wide singleton assumption;
  typed immutable value + callback session can be reused by future target adapters.
  Future multi-profile/group and override ownership is recorded rather than coded.
- Content changes may remeasure; current highlight only changes Background,
  selected focus outline only Opacity. Existing no-per-tick-measure tests pass.
  Huge content beyond screen size, all DPI/monitor combinations and final styling
  remain outside the evidence, as in the existing deferred overflow contract.
- Native keyboard/IME/focus, visual readability, double-click/F2 and title-bar
  behavior remain the explicit next checkpoint. No native acceptance is claimed.

## User native checkpoint

**Preparation record (subsequently completed below).** Under the user's revised AGENTS.md policy (2026-09-10), Codex first
attempts to launch the normal app directly with `--highlight-preview`, checks the
owned process and asks the user only for actual window visibility/visual UX.
Process presence, window handle and SessionId alone do not prove visibility.
The existing process-local 70-second synthetic clock does not change Windows time.
Manual host PowerShell launch is a fallback only after current execution/access/
desktop isolation evidence justifies it; past isolation is not sufficient.

Proceed one step at a time:

1. Confirm main window visibility.
2. Double-click a body cell; confirm target weekday/period and separate 교과/반.
3. Within the editor, continuously type Korean + Enter newline, Tab into 반,
   enter a class and Apply before returning to chat. Confirm exact selected cell
   display, adjacent cells unchanged and editor closed. Reopen with F2.
4. Edit both fields then Cancel; reopen and repeat with actual title-bar X.
   Check Escape cancellation, empty/class-only/whitespace/markup as needed.
5. Confirm header/highlight keeps moving during editing without retargeting Draft,
   and committed content/selection/current background coexist. Resize readability.
6. Close normally with X, confirm owned app process exits. Restart starts fresh
   (or reloads development fixture), since persistence is explicitly absent.

Native results will be appended with their exact evidence scope. Do not mark the
whole milestone or these native items complete before user observations arrive.


### Direct native launch under revised policy — 2026-09-10

- User changed AGENTS.md to direct-launch-first, with manual host launch only after
  current evidence of isolation, required OS input or unavailable execution access.
  The process-exists-does-not-prove-user-visibility rule is retained.
- Codex directly started the repository's Debug Desktop.exe with --highlight-preview
  and normal window style. Owned PID 59136, SessionId 1, WaitForInputIdle true,
  title `School Timetable Widget — 강조 검증 · 모의 시각 (70초 순환)` and a nonzero
  main-window handle were observed. The process remained running/responding at
  the subsequent check and stderr was empty. This does not establish user visibility.
- Diagnostics: C:\Users\ADMIN\AppData\Local\Temp\editing-native-8543fcfcbad140d89ebb384125e26692.
  No manual PowerShell launch was requested and no native key/pointer input was injected.
- User-visible window and editor UX confirmation is pending. Do not infer desktop
  isolation or native success solely from these process observations.

### Retry after actual nonvisibility report — 2026-09-10

- User reported that the first directly launched window was not visible and asked
  for another launch. This establishes nonvisibility for that attempt, not the
  exact cause or a universal inability to launch desktop apps.
- Verified PID 59136 by executable path and start time. CloseMainWindow returned
  true but the process did not exit within 2 seconds, so Codex stopped only that
  owned process for cleanup. This is not a normal-exit/native-X pass.
- Retried with exec_command require_escalated outside the sandbox, as authorized
  by the user's relaunch request. Start-Process used normal window style.
  New owned PID 60128, start 2026-09-10 11:32:38, SessionId 1; input-idle true,
  nonzero main-window handle and expected preview title were observed.
- Logs: C:\Users\ADMIN\AppData\Local\Temp\editing-native-host-ee42992f41cf458fbe1baf91062e475b.
  User visibility of this second attempt is pending; no manual launch requested.

### User native visibility — 2026-09-10

- User answered `보임` after the outside-sandbox retry (owned PID 60128).
  The second launch is confirmed visible on the user's actual desktop.
- This confirms visibility only. Double-click/F2, pair editing, IME, Apply/Cancel/X,
  focus/Tab, highlight coexistence and normal exit remain to be checked.
- For this environment/session, the outside-sandbox direct launch worked after
  the sandbox launch was reported invisible. No manual PowerShell was needed.

### User native editing checks — 2026-09-10

Confirmed on the visible outside-sandbox app (PID 60128):

- Double-click Monday period 3: user confirmed target 월요일 3교시 and separate
  교과=수학 / 반=2-3 inputs (`표시됨`).
- Requested continuous native input: replace subject with 물리학, Enter, 실험;
  Tab to class, enter 3-5 and click Apply. User confirmed three-line main-cell
  display (`세줄로 표시`). This is limited user-observed Korean/Enter/Tab/Apply evidence,
  not exhaustive IME composition coverage or pixel/count measurements.
- Single-click the same cell + F2: user confirmed subject 물리학/newline/실험 and
  class 3-5 retained as separate fields (`유지`).
- Changed both Draft inputs and clicked 취소; reopened with F2. User confirmed
  original subject/class remained (`그대로`).
- Changed both Draft inputs and clicked actual editor title-bar X; reopened with
  F2. User confirmed original subject/class remained (`유지`). This is native X
  evidence distinct from the earlier programmatic Close test.
- Exact counts of whitespace/codepoints and every untouched neighbouring slot
  remain primarily automated-test evidence. No screenshot or runtime text dump
  was supplied for these native checks; do not expand the user's short responses
  into exhaustive visual verification.
- Escape, editing-time header/highlight coexistence, resize and process exit checks
  are still pending at this point.

### User native coexistence/resize and Codex normal restart — 2026-09-10

- User answered `정상` to the continuous check: modify both Draft inputs, Tab to
  Apply, observe header/highlight for about 15 seconds, then Escape. The expected
  result was editor closure with original 물리학/newline/실험 + 3-5 retained.
  This is user-reported limited native coexistence/Escape evidence, not measured
  timer cadence, exhaustive transitions or IME composition-state coverage.
- User answered `정상` after narrowing and widening the main window to check the
  three-line cell, header/highlight, clipping/overlap/shifting. Limited resize
  smoke passed; no DPI/multi-monitor or pixel measurement is claimed.
- Codex verified the owned PID 60128 path/start time, requested CloseMainWindow
  from outside the sandbox, and observed exit within 10 seconds and remaining
  matching PID count 0. No forced termination. ExitCode was unavailable/null;
  do not report an observed exit code 0 or native title-bar click.
- Codex restarted the same executable/preview normally outside the sandbox.
  Owned PID 62984, start 2026-09-10 11:39:20, SessionId 1, input-idle true,
  expected title. Logs: C:\Users\ADMIN\AppData\Local\Temp\editing-native-restart-b7a79e1acf0740ec96606f6c62bcde9d.
- Fresh fixture values after restart still await user visual confirmation.

### Native acceptance and milestone completion — 2026-09-10

- After Codex restarted the app, user confirmed `초기값으로 돌아옴` for Monday
  period 3 (수학 / 2-3). Accepted edits were not retained across this new process;
  the explicit development preview fixture was reconstructed. This does not claim
  save/load or persistence implementation.
- Codex then verified PID 62984 by executable path/start time, requested normal
  CloseMainWindow outside the sandbox, and confirmed process exit and remaining
  PID count 0. No forced termination was used for either visible app shutdown.
  Final stderr length 0; exit code unavailable/null, not claimed to be 0.
- The Editing Foundation milestone is COMPLETE within the approved scope:
  Golden Reference investigation, updated Product/ADR/architecture, canonical
  SubjectText/ClassText pair, one-cell Draft/atomic in-memory Apply/Cancel,
  lossless plain strings, highlight coexistence, 331 automated cases, self-audit,
  and the native user checks documented above.
- Native scope: actual visible app, double-click/F2, separate field retention,
  Korean/Enter/Tab input, Apply-and-close, Cancel/X/Escape, editing-time refresh,
  limited resize, fresh-process reset. User observations are not expanded into
  exhaustive IME states, all time transitions, exact whitespace counts, all DPI/
  monitors, maximum content or pixel geometry measurements.
- Product code has not changed since the successful 331-case build/test run.
  Subsequent changes were native policy/evidence/future-requirement documentation.
  No additional code regression run was necessary solely for those documentation
  updates. Final diff/whitespace checks passed. No outstanding P1/P2 finding.
- Persistence, bulk import, Date Override/date display, teacher/group models and
  multi-tab UI remain unimplemented future milestones. No commit/push was requested
  for this milestone; changes remain in the working tree.
