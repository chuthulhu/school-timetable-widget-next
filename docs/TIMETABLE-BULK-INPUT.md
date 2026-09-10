# Bulk Timetable Input

Date: 2026-09-10
Status: **COMPLETE — AUTOMATED VERIFIED / USER NATIVE UX APPROVED (limited scope below)**
Authority: explicit two-stage user request, [ADR 0007](adr/0007-bulk-timetable-input.md).
This record distinguishes implementation/STA tests from native user evidence. Preparation and pending entries below are chronological history; the final acceptance section supersedes their status.

## Stage 0 publication

Editing Foundation diff matched the supplied scope; no unrelated change found.
Restore/build/331 tests passed, 0 warnings/errors; git diff --check passed.
Commit `9cae616cf24721423eacecd4d0aaa1100cf8ed69`:
`feat: add subject and class timetable editing`.
Normal fast-forward push to chuthulhu/school-timetable-widget-next origin/main
succeeded (fa0b492 → 9cae616); HEAD == origin/main and clean tree verified before
Stage 1. No reset/revert/force/remote/credential/system configuration change.
The environment lacked Git author configuration: the consistent author of the
last three main commits was used with command-local -c options only.

## Clipboard table parsing

Core Features/TimetableImport/ClipboardTable is a pure state-machine parser:
start/unquoted/quoted/closed-quote states recognize TSV separators, CRLF/LF/CR,
doubled quote escapes, quoted tabs and exact embedded line endings. Empty and
trailing cells remain data. A final record separator ends the row; additional
empty rows remain rows and must satisfy rectangular validation. Stray/unclosed
quotes and nonrectangular data reject. The writer quotes fields as needed.
No HTML interpretation, trimming of data, spreadsheet API/library or WPF reference.
WindowsSpreadsheetClipboard is a small Desktop STA boundary with read/write text
operations; tests substitute memory text without accessing the user's clipboard.
Plain Unicode TSV is supported; HTML-only clipboard data and file parsing are not.

## School Timetable Import

Search every row/column for 35 contiguous literal header numbers, 1–7 repeated
five times. No absolute source-column index. A unique signature is required;
multiple signatures reject rather than selecting the first. The immediately
preceding weekday header is validated if recognizable: Monday–Friday, each
seven columns, either merged-cell blanks or repeated weekday labels.
Conflicting/partial weekday evidence rejects. Missing weekday evidence produces
a visible confirmation notice, never an automatic rejection or silent acceptance.

Body rows immediately below the period header must be complete consecutive pairs.
Explicit 교과/반 markers outside the region or populated teacher/number metadata
followed by blank continuation provide pair evidence. Metadata header names are
교사/교사명/성명/번호; they may appear anywhere outside the region above the data.
Name values label candidates only. Subject vocabulary and class-name regexes are
never used. Every actual timetable field is passed unchanged to SubjectText or
ClassText. A completely empty class row is valid; its row must still be copied.

Exactly two isolated rows without pair markers are offered with an explicit
row-order warning. Multi-pair input without structural evidence, an odd body row
count, or a second teacher identity in a proposed class row rejects the whole
input. There is no skipping unknown rows or partially accepting an earlier pair.
This is a conservative supported subset of school formats, not a universal school
spreadsheet detector. Unsupported single-row teachers, extra footer/body rows,
or repeated-name continuation formats should use the canonical fallback.

School candidates are never automatically selected, including a single candidate.
Users select one, inspect the 35-slot preview, confirm weekday and subject/class
mapping, then Apply. Changing candidates resets confirmation. Labels include
source row numbers; evidence includes the exact source column span. Multiple
teachers are candidates for the current active week, not new profiles/tabs.

## Canonical Template Import / Copy

Exact 8×11 shape: header 교시 followed by 월-교과/월-반 through 금-교과/금-반;
data row labels must be the literal ordered 1–7. Header or period whitespace,
missing/reordered/duplicate periods and extra structure reject. Recognition does
not reuse School heuristics. Result is one immutable, deterministically ordered
35-cell week. Content, including empty/whitespace-only/Unicode/newlines, is exact.

Context-menu 표준 양식 복사 writes a blank canonical TSV only on explicit user
command and reports success/failure. Paste into spreadsheet A1, fill it, copy all
8×11, then choose 표준 양식 가져오기. No .xlsx export or external service involved.

## Common Preview and Atomic Apply

TimetableImportCandidate holds an immutable WeeklyTimetable plus display-only
label/evidence. Canonical domain fields remain SubjectText/ClassText and slot
identity remains SchoolDay/PeriodNumber, ordered period first, Monday–Friday.

TimetableImportSession owns candidates, selection, independent preview and a
captured target callback. It has terminal Apply/Cancel commands. Read/parse/
selection/confirmation cannot mutate the active model. Failed re-read clears old
candidates and preview, disabling stale Apply. Errors remain visible in the dialog.
The dialog shows explicit mode, selected source, weekday/period headers, 35 pairs,
and whole-week replacement/in-memory lifetime. Separate TextBlocks preserve field
boundaries; long content scrolls. It has no cell editor or paste interception.

TryReplaceTimetable first verifies the captured baseline and no active cell edit,
prepares every value/display and change flag, then swaps the accepted week and
updates all stable cell VMs before the first notification. Reentrant commit during
publication is blocked. Invalid/stale target fails before mutation. All operations
are synchronous on the owning UI dispatcher; no timer is introduced. Arbitrary
throwing event subscribers are outside the transaction callback contract; even at
notification time all 35 accepted values and projections have already changed.

Cancel, X and Escape discard the import session; Apply closes only on success.
The ordinary double-click/F2 editor continues to target canonical pairs after bulk
Apply. Current highlight retains its stable cell VM/day-period identity and uses
the unchanged shared Application Clock/CurrentStatusRefreshLoop. Content may change
measurement; highlight transitions themselves remain background-only.

## Actual WPF entry

Timetable context menu: 학교 시간표 가져오기…, 표준 양식 가져오기…, 표준 양식 복사.
Cell tooltip explains right-click and Ctrl+V. View-scoped Ctrl+V enters School
mode only. The separate owned editor Window has no import key or command binding;
its TextBoxes retain normal paste. No permanent toolbar or grid geometry redesign.
Open reads clipboard once; 클립보드 다시 읽기 retries without leaving the dialog.

## Verification and evidence limits

Initial expanded suite: 408 passed, 0 failed/skipped; build 0 warnings/errors after
two test assertion-style warnings were corrected. Additional final verification
is recorded below. Existing 331 cases were retained. New cases cover parser,
structural fixtures, strict canonical validation, stale/rejected/atomic Apply,
selection/confirmation, clipboard failure and command wiring.

WPF tests create unshown windows on STA dispatchers, materialize bindings, use
commands/events and Measure/Arrange. They never Show windows, inject OS keys or
pointer input, access the system clipboard, or change the system clock/profile.
Programmatic Close is not native title-bar X; command routing is not native
Ctrl+V/IME/focus evidence. Geometry checks establish highlight invariance after
content replacement, not that changed text can never affect content measurement.

## Native checkpoint preparation

Opt-in `--bulk-preview` uses the existing process-local highlight clock and initial
editing fixture. Both import entries initially load anonymous deterministic sample
text through the real parser, labeled in the dialog title as development samples.
No clipboard read/write occurs for that initial sample; 클립보드 다시 읽기 explicitly
uses the real adapter. Normal startup always uses the actual clipboard.
Sample A/B candidates include paired and empty fields, multiline, literal markup
and whitespace. Canonical sample uses the actual canonical schema. This allows
visual review without silently replacing the user's clipboard. Template copy
still writes only when the user explicitly invokes the production menu action.

After automated validation/self-audit, Codex will directly launch the normal WPF
app outside the sandbox, reusing the path whose actual visibility was confirmed
in the earlier milestone. Process/handle/input-idle evidence alone does not prove
visibility. User checks proceed one step at a time, beginning with actual visibility.
Native readability, entry discoverability, pair mapping, candidate switching,
Cancel/Apply, grid/highlight stability, copy/import, resize and normal exit remain
pending. No native acceptance, completed milestone or Stage 1 push is claimed.

## Deferred

Persistence/restart retention, multi-teacher profile creation, groups/tabs,
Date Override/date-specific targets, semester sets, Settings, KRISS, tray,
installer, backup and migration remain unimplemented. Fresh startup reconstructs
empty/default data (or the explicit development fixture).

## Final automated verification and self-audit before native checkpoint

- restore → build --no-restore → test --no-build --logger console;verbosity=normal:
  all exit 0; **408 passed, 0 failed/skipped**, **0 build warnings/errors**.
  Existing 331 + 77 new cases. Final log:
  C:\Users\ADMIN\AppData\Local\Temp\bulk-final-26ec7f0702d24573b83654e03e46b301.log.
- Added context-menu materialization check verifies all three CommandTargets resolve
  to the timetable View; fake-adapter copy command executes without showing UI.
- Self-audit inspected every new Core/Desktop importer/session/view/adapter and
  target change, existing editor/highlight/refresh integration, tests and contract.
  No outstanding P1/P2 finding in this scope.
- No absolute timetable source column: scanned signature determines it. No actual
  timetable data trim/normalize: School Header helper is used for structural
  comparisons only. Subject/Class remain distinct and exact throughout preview.
- No subject/class regex, semantic teacher-name identity, auto-selected School
  candidate, partial parse result, silently skipped body row or mixed recognizer.
  Canonical headers, ordered periods and dimensions remain strict.
- All candidate weeks already satisfy 35-slot validation. Commit rejects stale
  baseline or active edit before mutation and publishes all values/projections
  before notifications. Cancel/read errors retain original; old candidates clear.
- Core project references/packages unchanged and has no WPF/platform API usage.
  Clipboard class calls reside only in Desktop Infrastructure/Windows.
- Highlight remains day/period based on stable cell VMs, existing shared clock and
  background-only style; single-cell editing works after import. No new clock read,
  global state, persistence, profiles, date override or out-of-scope service.
- Tests use anonymous deterministic fixtures and unshown STA object/event/layout
  evidence. Actual clipboard, native Ctrl+V/IME/focus/title-bar and readability
  remain the next user checkpoint, not implied by automated passes.

### Direct native launch — awaiting user visibility

Codex directly launched the Debug Desktop executable outside the sandbox with
`--bulk-preview`, normal window style. Owned PID **74956**, start time
2026-09-10T13:25:38.0724846+09:00, SessionId 1, input-idle true, running with a
nonzero window handle and the expected development-sample/mock-clock title.
Logs/process record:
C:\Users\ADMIN\AppData\Local\Temp\bulk-native-24123aad19e9434784bfc8246c3724d5.
No clipboard overwrite, pointer/key injection or system settings change occurred.
These process observations do not establish visibility or user UX acceptance.
Actual user visibility is the pending first checkpoint. Stage 1 remains uncommitted.

### User native visibility confirmed

The user answered `보임` after the direct launch of PID 74956. This confirms
actual visibility on the user's Windows desktop. Import entry, candidate/preview,
Apply/Cancel, clipboard workflow and final UX acceptance remain pending.

### User native School import and editing observations

- User answered `읽힘` after selecting sample teacher A and checking the Monday–Friday,
  periods 1–7 preview with subject above class. Limited user-reported readability.
- User screenshot showed sample teacher B / source rows 5–6 selected, subject 국어,
  separate gray class labels, literal <b>과목</b>, empty/class-only cells and scrollable
  preview. The first visible cell's subject is intentionally 국어 + newline + 실험;
  its class 3-1 is separate. This is deterministic test content, as explained to the user.
  Screenshot: C:\Users\ADMIN\AppData\Local\Temp\codex-clipboard-20fd01d2-a6af-4f3f-8ea0-0a7e89fcd4bc.png.
  The screenshot shows periods 1–5 with further rows available by scrolling; it does
  not alone establish visibility of all 35 cells or exact whitespace counts.
- After Cancel, user answered `그대로임`: original main timetable retained.
- User answered `ㅇ` to the requested School B selection, mapping confirmation,
  35-cell Apply and main-grid/highlight stability check. This records limited native
  Apply/display/geometry acceptance, not pixel measurements or every clock boundary.
- User answered `수정함` after the requested F2 single-cell subject/class edit and
  Apply following bulk import. This establishes reported editing completion; exact
  unchanged-neighbour counts remain primarily automated evidence.
- Canonical sample, actual explicit template clipboard copy/read, resize, exit and
  overall native UX acceptance remain pending. Stage 1 is still uncommitted.

### User native Canonical clipboard workflow, resize and UX acceptance

- User answered `자연스럽게 읽힘` after opening Canonical import: subject/class
  distinction and Monday–Friday layout read naturally in the development sample.
- User explicitly invoked 표준 양식 복사 and answered `ㅇ` to the copy-success
  message check. This is an intentional native clipboard write by the user action.
- User then opened 표준 양식 가져오기 and clicked 클립보드 다시 읽기, answering
  `ㅇ` to the check that the development sample was replaced by the empty canonical
  template without validation error. This exercises actual copy → Unicode clipboard
  read → strict parser → preview. No Excel/Google Sheets roundtrip was performed;
  broader quoted-text preservation remains parser/adapter test evidence.
- After the requested 35-cell Apply, user answered `계속 보임` to the check for
  continuing current-cell highlight. That response specifically confirms highlight
  visibility; exact all-35 empty values are established by the automated contracts,
  not inferred as a separately measured native count.
- User answered `ㅇ` after the final request to narrow/widen the app and confirm
  stable header/grid/highlight and acceptance of the import/candidate/template UX.
  **Native UX approved.** No style adjustment was requested.
- Scope is limited user observations and the supplied School B screenshot. It is
  not exhaustive native Ctrl+V/IME/focus, import title-bar X/Escape, every period,
  all monitor/DPI or pixel geometry evidence. Programmatic X/command policy tests
  remain distinguished from actual OS input. Persistence remains unimplemented.

### Owned process normal shutdown

After native acceptance, Codex verified executable path and exact start time for
owned PID 74956, requested CloseMainWindow outside the sandbox, and observed
normal exit within 10 seconds and remaining owned PID count 0. No force termination.
Stderr length 0. ExitCode was unavailable/null; no observed exit-code-zero or
physical title-bar X click is claimed. The first identity-check attempt made no
close request because PowerShell JSON parsing auto-converted the saved ISO date;
reading the record with DateKind String confirmed exact identity and allowed the
normal shutdown. This was a diagnostic comparison issue, not an app failure.

## Final verification after native acceptance

- Restore, build --no-restore and test --no-build --logger console;verbosity=normal
  rerun after native approval and shutdown: all exit 0. **408 passed, 0 failed,
  0 skipped; build warnings 0, errors 0**. Log:
  C:\Users\ADMIN\AppData\Local\Temp\bulk-accepted-4d14eb9d58984ea3a750442bb3f85118.log.
- Final self-audit reconfirmed lossless parsing, explicit/separate recognition,
  no automatic School selection, immutable complete 35-slot candidates, atomic
  target replacement, no stale re-read payload, unchanged canonical fields/current
  identity and editor integration. No outstanding P1/P2 finding.
- Core/platform separation and absence of persistence/profile/date/semester scope
  additions rechecked. No tests disabled or skipped. Product source did not change
  after user native approval; only evidence/status documentation was updated.
- git diff --check and whitespace inspection of new files passed before staging.
- Both automatic and required limited native gates are satisfied. The user's
  original authorization permits committing this milestone and a normal
  fast-forward push to origin/main. No force push or configuration change allowed.
