# Display Settings / Presets Foundation

Date: 2026-09-11. **IMPLEMENTED — NATIVE REVIEW CONFIRMED**.
Authority: user milestone request; [ADR 0014](adr/0014-display-presets-and-schema-v2.md).
Start: main, HEAD/origin/main 45418fe31aade3634419946837e98b28a3ba5c86, clean.
No branch switch/reset or remote/system-setting change.

## User behavior

시간표 또는 시계 영역을 우클릭해 **표시 설정...**을 열거나 **Ctrl+,**를 사용합니다.
표시 스타일을 고른 다음 시간·날짜·요일·상태의 글꼴, 글자 크기, 굵기, 기울임을
각각 바꿀 수 있습니다. 변경한 내용은 메인 화면에 즉시 미리 표시됩니다.
적용은 저장하고 창을 유지하며, 확인은 저장 성공 후 닫습니다. 취소/X는 마지막으로
적용한 설정으로 돌아갑니다. 기본값으로는 현재 선택한 표시 스타일만 초기화해 미리 봅니다.

| Style | Initial layout / values |
| --- | --- |
| 표준 | Date, time, full status on one row; 16 / 16 / 16; 24-hour with seconds |
| 디지털 | Centered large time (48), separate date (14) and full status (15) rows |
| 컴팩트 | Shorter one-row time (16) and full status (14); date and seconds initially hidden |
| 미니멀 | Time (24); date, status and seconds initially hidden |

All presets initially hide weekday and use Segoe UI, normal style. Digital/Compact time
starts at medium weight. These are editable initial values, not font restrictions.
Compact retains full status/countdown semantics; the user's abbreviated example is not
a new Core status mode. Enabling date/weekday/status works in every layout independently.
12-hour format has Korean 오전/오후, midnight/noon 12 and unpadded 1–11.
Date remains yyyy년 MM월 dd일. Column M/d and short weekday labels do not use these settings.
Time size 10–96, supporting sizes 8–48, finite values only, in WPF device-independent units.
Weight choices are 얇게/보통/중간/굵게; style choices 보통/기울임.

System-installed family names are selectable, including user-installed families that WPF
enumerates. An editable picker retains missing names and shows a short fallback notice.
The Windows adapter resolves locally to Segoe UI or the system message font if missing.
Availability never changes the canonical family identity or makes a profile invalid.

## Ownership and safety

- DisplayConfiguration is immutable Desktop data, separate from WPF, Core and JSON DTOs.
  Preset, layout and each element's typography are independent. FontSelection explicitly
  names its source; new sources can be introduced without a provider hierarchy.
- RuntimeDisplaySettings owns committed/current preview and one active display session.
  DisplaySettingsSession owns replaceable element Drafts and last successful Apply baseline.
  Invalid fields block Apply and retain the last valid preview; errors are user-facing Korean.
  Failed save retains old disk/committed/baseline and editable Draft/preview.
- ProfileSession saves the latest complete candidate before publishing commitment. Every
  other feature save retains committed Display, never current Preview. Display Cancel does
  not undo successful timetable/schedule/date/lunch saves.
- Formatter output remains independent CurrentDateText/CurrentTimeText/StatusText. Weekday,
  AM/PM and 12-hour text derive from the same existing snapshot. The ViewModel reformats
  cached facts on preview; no clock read, timer, tick font enumeration or persistence.
- The Header view retains its controls. Explicit configuration changes reserve fixed text
  slots/height and remeasure WindowContentMinimum; ticks do not reconstruct layout.
  Digital lines center inside stable slots. Oversized width uses horizontal scrolling.
  Normal resizing remains available. Full persisted preferred geometry is not implemented.
- Week arrows use a local rounded hover/pressed/focus template with ‹/›, no icon package.
  Dates have stronger visual weight than weekdays; Today changes background only.
  Current body highlight is unchanged, with no geometry-affecting trigger.

## Persistence v1/v2

Schema 2 stores required profile.display: preset, layout, time/date/weekday/status
(font source/family, size, weight, style), use24Hour, showSeconds, showDate, showWeekday,
showStatus. Canonical values use names; paths, unknown enums, missing/null/extra fields
and invalid sizes are rejected safely. No derived current text or viewed week is stored.

A separate strict v1 DTO reads the previous exact shape. All timetable strings, tick-precision
period times, independent date overrides and lunch survive; Standard defaults are injected.
v1 load remains writable and never rewrites the file. Any next successful user save writes
v2. The atomic write/lease/external-change protection is unchanged. There is no downgrade
support; old v1-only apps may treat v2 as unsupported.

## Verification scope

Automated tests cover preset defaults, independent overrides/reset, formatting including
midnight/noon, options, invalid input, missing fallback and no extra clock reads; P2 preview,
Apply/OK/Cancel, failed-save retry, one-editor ownership and cross-feature preservation;
fixed v1 fixture exact load/no rewrite, next-save upgrade, v2 round trips, strict invalid
display rejection, missing-font portability and all four atomic write fault stages.

Unshown WPF object/event tests cover selectors, actual binding changes, independent controls,
preview, buttons/X closure, layout switches, visibility/font properties, command routing,
arrow styles, extreme sizes and unchanged header/35-cell geometry across ticks/resizes.
Existing Today/highlight/week navigation and original regression tests remain.
These are object/event/layout observations, not actual keyboard/IME/OS/native UX evidence.

Temporary offscreen RenderTargetBitmap images are used for layout inspection only. They
do not establish inactive/occluded/native equivalence. No native input, pointer movement,
clipboard change or production profile access is used by automated verification.

Final command results and native launch/user acceptance are appended below when observed.
Commit and fast-forward origin/main push require user native UX approval first.

## Deferred

| Feature | Status |
| --- | --- |
| Display Presets | IMPLEMENTED — NATIVE REVIEW CONFIRMED |
| Per-element typography | IMPLEMENTED — NATIVE REVIEW CONFIRMED |
| System font selection | IMPLEMENTED — NATIVE REVIEW CONFIRMED |
| Bundled font catalog | PLANNED |
| Online font catalog/download/cache | PLANNED |
| Named custom presets | PLANNED |
| Color/theme editor | PLANNED |

Local font file import, independent Title/AM-PM controls, final Fluent redesign and whole
responsive layout framework are also not implemented. No new dependency or font download.

## Native review checklist

Codex directly launches the normal app with a unique TEMP profile after automated validation.
Confirm actual visibility, Standard familiarity, Digital emphasis, Compact height, Minimal
balance, immediate preset preview, system font selection, independent element sizes,
12/24-hour/seconds/visibility, Cancel and last-Apply restoration, Apply/restart retention,
arrows, Today and resize. Codex observes owned process lifecycle and persisted data.
Human UX approval is pending; no native success or commit/push is claimed before it.

## Automated validation and self-audit — 2026-09-11

- dotnet restore and dotnet build --no-restore exited 0; build warnings/errors 0.
- dotnet test --no-build --logger "console;verbosity=normal" exited 0:
  **695 passed, 0 failed/skipped** (baseline 623 plus 72 new cases).
- Log: C:/Users/ADMIN/AppData/Local/Temp/display-final-6e30524dcf9c4354ba562bcf0cef000a.log.
- Fixed embedded profile-v1.json verifies original v1 shape independently of the current
  serializer. Full original JSON inputs survive in-memory load and next-save v2 upgrade;
  startup bytes/mtime stay unchanged and the profile remains writable.
- Earlier baseline expectation changes only reflect approved v2 schema, added display
  properties/menu, new chevrons and the configurable header containers. No original test
  was removed/skipped; clock counts, data safety and geometry assertions remain.
- Final source/transaction/scope self-audit found no outstanding P1/P2 finding in the
  inspected scope. Independent element inputs, layout/typography separation, logical font
  identity/fallback, P2 rollback/failure, all-feature save preservation, strict v1/v2,
  atomic-write regression and tick stability were checked. No extra clock read, Core
  display model, downloaded/bundled font, custom preset manager or global Settings state.
- Temporary offscreen inspection found the four layouts distinguishable and informed
  Digital text centering. Rendering is unshown WPF evidence, not native acceptance.
  Images: C:/Users/ADMIN/AppData/Local/Temp/display-review-68221bcae50b4e3eb460aac5652c9c2d.
- A final whitespace check initially found added EOF blank lines. They were removed;
  touched text files were normalized to the repository's Windows line endings.
  git diff --check subsequently exited 0. This changed no program behavior.
- Native review and human UX approval are still pending. No commit/push performed;
  main HEAD remains 45418fe31aade3634419946837e98b28a3ba5c86.

## Direct native launch — awaiting visibility confirmation

The normal Debug app was directly launched outside the sandbox at
2026-09-11T12:15:22.1139255+09:00. PID 64984, expected executable/title, input-idle true,
running with window handle 2494506; stderr length 0. These observations establish startup,
not actual visibility on the user's desktop.

TEMP profile: C:/Users/ADMIN/AppData/Local/Temp/display-native-87c37e43d83f4e5d80f716057d0b3d7b.
Arguments: --timetable-preview and that --dev-profile-directory. This uses sample timetable
data with the normal shared PC fallback clock; no synthetic clock or altered window flags.
No production profile, system settings, native keys/pointer or clipboard were changed.
The user must confirm actual window visibility and the requested native UX; restart/normal
closure and saved display retention will then be observed using this same isolated profile.
Native approval, final post-approval verification, commit and push remain pending.

### Actual desktop visibility confirmed

The user answered "보임", confirming the directly launched app is visible on the actual
Windows desktop. Codex rechecked PID 64984, exact executable/start timestamp and responsive
state; stderr remains empty. The diagnostic reader preserves the JSON timestamp as a string
to compare the recorded identity without PowerShell's automatic date conversion.

This confirms visibility for this launch only. Display preset readability, native interaction,
restart retention and overall UX approval are still pending.
## Direct computer-use verification — 2026-09-11

The user explicitly requested direct computer control. During that authorized foreground
interval, Codex used computer-use 26.903.71938 / @oai/sky against the normal app and the
same isolated TEMP profile. Mouse/keyboard input was used in this interval; the earlier
background-only verification description does not describe these native steps.

Observed native successes:

- Ctrl+, opens the owned display settings dialog. Standard header and the 35-cell grid
  render with Today and the current period highlighted; maximization renders the full grid.
- 24-hour, seconds, date, weekday and status checkbox clicks update the header immediately.
  Weekday can remain visible independently of the date. Cancel restores all original
  Standard values. Before Apply, profile.json remains absent after this preview/cancel run.
- Next week shows September 14–18 and removes Today/current-period highlighting. Previous
  week restores September 7–11 and Friday/current-period highlighting.
- Apply with seconds disabled persists schema v2 and leaves the dialog open. Re-enabling
  seconds as an unsaved preview and closing the dialog with title-bar X restores the last
  applied seconds-disabled state. The saved profile still contains all 35 timetable cells.
- Main title-bar X normally closes PID 64984; Codex independently verifies process absence
  and empty stderr. No forced termination is used as evidence.
- Restart of the same executable/profile at 12:28:05 +09:00 creates PID 64972. The header
  and reopened settings retain seconds disabled. Profile SHA-256 is unchanged on startup:
  1C0C94F4BD9C9D3A903DD82CAE59753CF77993EE6EAA3ACC763EB95500A948CC.
- Reset restores Standard defaults in the preview. OK saves seconds enabled and closes the
  settings dialog. Restart stderr remains empty. The restarted main window is left open
  for the user's review, using only the diagnostic TEMP profile.

Tool limitations and remaining evidence:

- The owned modal is captured and appears in the accessibility tree, but list_windows and
  list_apps expose only the main window as a target. Modal element click/set_value returns
  'element ... is not available in cached app state'. Screenshot checkbox/button clicks work.
- The preset dropdown opens and visibly lists all four presets. A subsequent popup click
  or arrow key targeting the only exposed main window closes the popup without changing
  Standard. Input activates the main window; this also changed its placement/restore state
  during one maximized-dialog attempt. These are observed automation limitations, not an
  established application defect. No fabricated modal target or changed production flags
  were used to bypass them.
- Native preset selection, per-element font/size typing, keyboard/IME and focus behavior
  remain unverified. Existing object/event tests and offscreen preset renders cover their
  stated scope only; they do not substitute for these native behaviors or human UX approval.

Human UX approval is still pending. No commit or push has been made. The source behavior
has not changed during this native run; only the verification record has been updated.
## User native confirmation and final acceptance — 2026-09-11

The user confirmed: "프리셋 선택·글꼴/크기 입력의 실제 조작 확인함".
This closes the remaining preset-selection and font/size native-operation review items
identified in the direct computer-use report. Together with the preceding native smoke,
this is recorded as the user's UX acceptance for this milestone. These specific interactions
are user-reported evidence, not successful Codex automation. No broader keyboard/IME or
focus-loss coverage is inferred. Earlier pending statements above describe historical stages;
this section and the current status tables supersede them.

The post-acceptance build initially encountered MSB3026/MSB3027/MSB3021 because the owned
TEMP review app still held its executable open. Codex verified PID 64972's executable and
exact start time, requested normal closure with Process.CloseMainWindow, and observed exit
within 10 seconds with empty stderr. This process API closure is not additional native-X
evidence. No forced termination or unrelated process action was used.
Final post-acceptance verification passed: dotnet restore, dotnet build --no-restore,
and dotnet test --no-build --logger "console;verbosity=normal" all exited 0. Build warnings
and errors: 0. Tests: 695 passed, 0 failed/skipped; 72 new cases over the 623-test baseline.
Log: C:/Users/ADMIN/AppData/Local/Temp/display-approved-final-235bc3728b5a4b479c96787cfb24b894.log.
The final self-audit revisited P2 success/rollback boundaries, preserved profile data, strict
v1/v2 DTOs, system-font fallback and same-snapshot display rendering; no outstanding P1/P2
finding was identified within this inspected scope. git diff --check passed. Bundled/online
fonts and the other explicitly deferred capabilities remain planned. Commit and ordinary
fast-forward push are authorized by the original request after this accepted verification.

## User-defined display presets — 2026-09-11

**IMPLEMENTED — NATIVE REVIEW CONFIRMED**. Rename/update/delete and preset transaction
persistence have the same accepted status. Authority: user milestone;
[ADR 0015](adr/0015-user-display-presets-and-schema-v3.md).
Start: main HEAD/origin/main 8cdcf6449b697b7fcfabfcf4728375e9fb391b5d, clean.

표시 스타일에서 **기본 제공 · …**와 **내 프리셋 · …**를 구분합니다.
**내 프리셋으로 저장...**은 현재 설정을 이름과 함께 임시 목록에 추가하고 선택합니다.
이후 글꼴/크기 등을 바꿔도 원본은 그대로입니다. **기본값으로**는 현재 선택한 스타일의
저장값을 미리 봅니다. **이름 변경...**은 이름만 바꾸며 **현재 설정으로 업데이트**를
직접 눌러야 원본 설정을 교체합니다. 기본 제공 스타일은 변경할 수 없습니다.

**내 프리셋 삭제...**에서 삭제할 항목을 선택하고 이름을 확인한 뒤 **삭제**합니다.
현재 사용 중인 항목은 삭제가 막히며 다른 표시 스타일을 먼저 고르도록 안내합니다.
삭제 목록 선택은 현재 표시 스타일을 바꾸지 않습니다. 이름은 앞뒤 공백 제거/NFC,
60 UTF-16 단위 이내, 빈 이름/제어문자 금지, 내 프리셋끼리 대소문자 무시 중복 금지입니다.

추가·이름 변경·업데이트·삭제 모두 **적용/확인** 때 표시 설정과 함께 한 번에 저장됩니다.
**취소/X**는 마지막 적용 이후의 표시 미리보기와 프리셋 목록 변경을 모두 되돌립니다.
저장 실패 시 기존 저장값을 유지하고 편집 내용을 남겨 재시도할 수 있습니다.
Schema v3 saves stable identities and all display inputs without font files. Strict v1/v2
compatibility, complete validation and existing missing-font fallback remain in effect.
Preset import/export, bundled fonts and online font catalog remain **PLANNED**.

Verification results will be recorded after execution. Automated WPF object/event checks
are distinct from native keyboard/IME, OS focus behavior and human readability/UX approval.

## User preset automated validation and self-audit — 2026-09-11

- dotnet restore, dotnet build --no-restore, dotnet test --no-build --logger
  "console;verbosity=normal": exit 0; **751 passed, 0 failed/skipped**, build warnings/errors 0.
  Baseline 695 retained with 56 new cases. Log:
  C:/Users/ADMIN/AppData/Local/Temp/user-presets-validation-be9a895300ad47faab1d66cc63b2ffe5.log.
- Existing assertions were adapted to the explicit preset reference and schema-v3 writer;
  no baseline tests were removed/skipped. One initial failure was an unchanged week-navigation
  expectation of schema 2; the assertion now checks schema 3 while retaining the original
  no-write/navigation/override invariants.
- Fixed v1 and v2 fixtures verify writable/no-rewrite load and exact full-profile semantics,
  including tick precision, date components and missing-font identities. New v3 cases cover
  multiple Korean/Unicode names, duplicate IDs/names, missing/null/invalid payload fields,
  invalid font identity, forbidden binary fields, mixed and dangling references.
- Model/transaction tests cover all input capture, immutable values/copy isolation, stable
  rename identity, explicit update, active-delete rejection, Reset, each library mutation's
  Cancel rollback, retryable Apply failure at all four atomic write fault stages, and other
  feature saves preserving committed library while excluding Draft. Current library and
  display restart/selection/Reset are verified against real isolated TEMP profile files.
- Unshown WPF object/event tests exercise actual name/deletion windows through an injected
  dialog presenter, validation messages, selectors, management enablement, rename/update,
  inactive selection plus explicit delete confirmation, Apply/file persistence and Cancel/X.
  These do not establish native text entry, IME, focus behavior or readability acceptance.
- Source self-audit found no outstanding P1/P2 finding in the inspected scope. No mutable
  Draft references enter templates; built-ins remain immutable; rendering reads explicit
  configuration; one typed callback submits display/library together before publication.
  Strict v1/v2 readers, complete v3 validation, missing-font fallback and every other feature
  save boundary were reviewed. No Core/clock changes, startup rewrite, font binaries,
  import/export, online/bundled font implementation or generic framework was added.
- git diff --check passed. Commit/push remain gated on user native UX approval.

## User preset direct computer-use smoke — partial, 2026-09-11

The user explicitly requested direct computer control for native verification. During this
foreground interval Codex used computer-use 26.903.71938 / @oai/sky. This was not background
input. The normal Debug executable was launched directly outside the sandbox with
--timetable-preview and an explicit isolated --dev-profile-directory. Production window
flags and the normal PC-fallback Application Clock were unchanged; no system clock or
production profile was changed. Initial data was the fixed v1 test fixture with 35 cells,
precise schedules, date overrides and lunch enabled.

TEMP directory: C:/Users/ADMIN/AppData/Local/Temp/user-presets-native-a376222eced2477d8105f22caa57b181.
First PID 80948, started 2026-09-11T12:51:06.5859415+09:00; input-idle true.
The window was returned by the computer-use tool, and native screenshots/accessibility
showed the normal main window and owned settings/name windows. This establishes capture
success for this run; final user desktop visibility/readability/UX approval is still pending.

Directly observed:

- Ctrl+, opens Display Settings. The selector shows the four 기본 제공 entries; rename,
  update and delete are disabled when the library is empty.
- Coordinate click opens Save As and focuses the name field. Clicking Save with an empty
  field shows the Korean validation message and retains the dialog. Name-window title X
  returns to Display Settings. No preset was created and profile bytes remained unchanged.
- Seconds checkbox changes Preview. Apply writes schema 3 and keeps Settings open. All
  original timetable/schedule/date-override/lunch inputs compare exactly to the v1 fixture.
- Reset enables seconds in Preview; settings title X restores the last applied no-seconds
  display. The saved bytes remain identical to immediately after Apply.
- Main title X closes PID 80948 normally; process absence and zero stderr were independently
  checked. No forced termination was used.
- Restart with the same executable/profile: PID 79944, started
  2026-09-11T12:56:51.3188585+09:00. Native capture shows retained no-seconds header and the
  timetable. Startup profile SHA-256 remains
  9AE67059EC4A484760B02942EE17FC1BD2516DE6E8994959EC270D788FE10EBE.
  Initial v1 SHA-256 was 706A8E4CCAD76494E167508A5B103C084225E67FD710FF4A708A450378F2808D.
  The restarted settings window is open for the user's remaining input review.

Observed automation boundary, not an established app defect:

- list_windows exposes only the main window; settings and name dialogs appear in captures
  and the accessibility tree but are not independent targetable windows. Element click on
  the observed selector returns 'element 131 is not available in cached app state'.
- Screenshot coordinate click opens the style popup, but clicking Digital activates the
  main window and dismisses the popup without changing Standard. No fabricated window
  handles or changed production flags were used.
- type_text targeted to the only exposed main window does not enter text into the observed
  focused NameInput. Codex explicitly clicked NameInput, observed caret/focus, then retried
  once; the field remained empty and the main window became active. Capture works; modal
  discovery/targeting and input delivery are the limiting steps. No native text/IME success
  is claimed. A displayed accessibility focus line alone was insufficient evidence.

Remaining user native review: Digital/font/size entry, Korean preset creation and selection,
user-template Reset, rename, explicit update, active-delete guidance/inactive-delete
confirmation, library Cancel rollback, and applying a user preset for restart confirmation.
Codex can inspect resulting profile data and perform owned normal restart; the user need
not run PowerShell or launch the app manually. User UX approval and custom-preset native
restart remain pending. No commit or push has been made.

## User-created preset and continued native smoke — 2026-09-11

The user reported saving the preset ("저잠함", understood as "저장함"). Codex then read
only the isolated diagnostic profile and captured the native app. It contains user preset
교무실 시계, stable ID 72544ad8-0664-4f2e-bde0-6a1f9730cd26, Digital layout, Eras ITC
Time family, size 48, Medium/Normal, with seconds enabled. The active reference points to
that ID and the complete display payload matches the saved template. Creation/name/font
entry is user-performed evidence; Codex did not automate those keystrokes. A size change
or broader IME coverage is not inferred. All original v1 timetable/schedule/date/lunch
inputs remain exactly preserved.

During the continuing authorized computer-use interval, Codex directly verified:

- Reopened Settings selects 내 프리셋 · 교무실 시계 and enables management controls.
- Temporarily disabling seconds then Reset restores seconds and the user font Eras ITC.
- Disabling seconds, explicitly clicking 현재 설정으로 업데이트, re-enabling seconds as a
  further preview, then Reset restores the explicitly updated seconds-disabled payload.
  The deletion picker accessibility value independently exposes that updated Draft payload.
- The deletion picker lists the user preset. Selecting it keeps Delete disabled and shows
  the instruction to select another display style first. No deletion was performed here.
- Closing the deletion picker and clicking Settings Cancel restores the original seconds-on
  display. Profile bytes remain identical to the user's saved baseline; no Apply was used
  during this temporary update test.
- Main title X normally closes owned PID 79944; process absence and zero stderr verified.
- Direct restart with the same executable/TEMP profile produces PID 83600 at
  2026-09-11T13:24:35.2847369+09:00. Native capture and reopened Settings show the retained
  user preset, Eras ITC size 48 and seconds enabled. Profile SHA-256 before temporary edits,
  after Cancel, and after restart is unchanged:
  CEF65CC61D5F9F4502B53A3ED17852878EC575493B30389FA167874126DD7BEA.

Remaining native/user review: switching away and reselecting the user template; actual
rename text entry; inactive-preset confirmed deletion and Cancel restoration; overall UX
approval. The observed modal targeting/input limitations still apply. Settings is open for
these steps. No source/test change was made in this continued native run. Commit/push and
post-acceptance final verification remain pending.

## User preset native acceptance — 2026-09-11

The user answered "정상작동" after the remaining checklist: switching to another style and
reselecting 교무실 시계, renaming it to 교무실 시계2, switching away, confirming deletion of
that inactive user preset, and cancelling Display Settings. This is recorded as the user's
successful native review and UX acceptance for this milestone, together with the prior
user-created preset and Codex's direct native observations. These particular selection,
rename and deletion interactions are user-reported; no automated keyboard/IME success or
broader OS-focus coverage is inferred.

Codex independently compared the diagnostic profile after this checklist with the original
user-saved baseline. Both SHA-256 values are
CEF65CC61D5F9F4502B53A3ED17852878EC575493B30389FA167874126DD7BEA.
The original preset name/ID/payload and all other saved inputs are therefore unchanged after
Cancel. Earlier pending statements are chronological records, superseded by this acceptance
and the current feature status declarations.

Before final build, Codex verified PID 83600's executable and exact start time and requested
Process.CloseMainWindow. It returned false and the initial 10-second wait did not observe
exit. A subsequent computer-use Ctrl+, / title-X attempt ended with 'foreground window did
not report a process id'; a process query then confirmed PID 83600 was absent. No forced
termination was issued. This last sequence is recorded as process absence with uncertain
input attribution, not an additional proven native-X success. Earlier directly verified
native-X shutdown/restart evidence remains valid. Final commands run after the executable
was released; no manual launch, production-data change or system-setting change is needed.

## User preset final post-acceptance validation — 2026-09-11

- dotnet restore, dotnet build --no-restore and dotnet test --no-build --logger
  "console;verbosity=normal" all exited 0. **751 tests passed, 0 failed/skipped;
  build warnings/errors 0**. This retains the 695 baseline plus 56 new cases.
- Log: C:/Users/ADMIN/AppData/Local/Temp/user-presets-approved-final-40648930722e4fbeb35281b6b2096d59.log.
- Final self-audit rechecked immutable payload/identity, selected-template Reset, library
  rollback, single display/library save-before-publish, committed-library preservation in
  all other saves, strict v1/v2/v3 validation and font fallback. No outstanding P1/P2 finding
  was identified in the inspected scope. Core, clock and production data remain unchanged.
- No production code changed after native acceptance; only approval and evidence documents
  were updated. A trailing documentation blank line found by diff-check was removed.
- User native acceptance is complete for the requested checklist. Broader IME, all-DPI,
  multi-monitor and general OS focus coverage is not claimed. Existing modal automation
  limitations and evidence attribution are preserved above.
- The original milestone authorizes committing and ordinary fast-forward push to
  chuthulhu/school-timetable-widget-next origin/main after these successful checks.

## P2 duplicate settings open — 2026-09-11 follow-up

Correction to the final prior-run attribution: post-push inspection of PID 83600's stderr
confirmed an unhandled InvalidOperationException ("표시 설정이 이미 열려 있습니다.") from
RuntimeDisplaySettings.Open via MainWindow's command handler. Its absence was a crash,
not normal shutdown. This supersedes the earlier uncertain-exit and no-outstanding-P2
statements; the separate earlier successful native-X evidence remains valid.

The handler called Open unconditionally while ShowDialog's nested dispatcher was active.
DisplaySettingsWindowOwner now guards every shared command entry before session creation,
activates the existing window and preserves Draft, Preview, library and Apply baseline.
Closed/finally release ownership; the session's existing save/cancel behavior and runtime
one-active-session invariant remain unchanged. No blanket exception handling, persistence,
schema, clock or unrelated feature change is introduced.

Validation status: regression and native checks in progress; this follow-up is not yet
complete or authorized for commit/push until both have passed.

Automated follow-up evidence:
- Nine new unshown WPF regression cases cover repeated commands, incomplete Draft/error,
  preview and library identity, last-Apply rollback, all three routed targets/context-menu
  bindings, fresh reopen and cleanup after a presentation exception. The existing 751
  tests remain; final restore/build/test passed **760/760, warnings/errors 0**.
- Final normal-verbosity log:
  C:/Users/ADMIN/AppData/Local/Temp/duplicate-settings-final-bf32367eb7f4431eb8ca203fca978f1b.log.
- The first full run had 759/760: existing UserPresetViewTests.SaveNameDialogValidatesThenSelectorAndManagementReflectUserPreset
  observed null SelectedValue after Save As. Its isolated rerun and the complete final rerun
  passed without source/test changes. The cause of that intermittent observation has not
  been established; it is not claimed fixed by this window-ownership change. First-run log:
  C:/Users/ADMIN/AppData/Local/Temp/duplicate-settings-validation-4213fc0641b04ac8bea3eacf291e18f1.log.

Native diagnostic started directly outside the sandbox with normal production window flags,
--timetable-preview and a unique TEMP --dev-profile-directory. PID 92248 started at
2026-09-11T14:37:21.8537901+09:00, window 8392858. TEMP directory:
C:/Users/ADMIN/AppData/Local/Temp/duplicate-settings-native-2333969b614c417d900c3d0286e8cdda.
The user-saved diagnostic fixture was copied there; production settings were not used.
Initial SHA-256: CEF65CC61D5F9F4502B53A3ED17852878EC575493B30389FA167874126DD7BEA.
Window discovery and screenshot/accessibility capture succeeded. The first Ctrl+, action
failed before input with "failed to activate captured window". After fresh discovery and
rehydration, one explicit activation retry returned the same error. Native input is paused
pending the user bringing that existing window forward; this is an activation failure,
not a launch/capture failure or a completed native regression.

Native continuation and completion:
- The user brought the existing window forward after the activation failure; Codex then
  performed the remaining inputs directly. No relaunch or manual test checklist was needed.
- Native Ctrl+, opened Settings. Codex unchecked seconds without Apply, observed the
  seconds-free header Preview, then sent Ctrl+, three more times. Each capture retained
  one Settings window (same accessibility entry 120), the same user preset/font and the
  unchecked seconds Draft. No crash, new window or reset was observed.
- Native right-click attempts on the exposed header/main and timetable areas while Settings
  was modal did not open a context menu or change the Draft/window. Thus native menu
  duplication was blocked by the modal interaction; execution of an already-open menu
  command is covered by the unshown routed-command tests, not claimed as a native click.
- Cancel closed Settings and restored seconds. Ctrl+, reopened a fresh window with saved
  seconds/preset/font. Its title X closed normally. With Settings closed, native header
  right-click exposed 표시 설정...; choosing that item opened Settings normally. Another
  Ctrl+, retained that window (accessibility entry 329), then its title X closed normally.
- Codex clicked the main title-bar X. A wait on the verified owned PID observed exit;
  subsequent process lookup confirmed PID 92248 absent. stderr remained **0 bytes**.
  ExitCode was unavailable from the attached Process object (null), so no numeric process
  exit-code claim is made. No force termination was used.
- TEMP profile SHA-256 and last-write time were unchanged during repeated opening and after
  shutdown: CEF65CC61D5F9F4502B53A3ED17852878EC575493B30389FA167874126DD7BEA,
  2026-09-11T04:22:57.4600817Z. No persistence write occurred. Original fixture untouched.
  run.json and exit-result.json in the diagnostic directory retain launch/exit evidence.

Follow-up status: implemented and native regression confirmed within the precisely stated
menu/modal evidence above. No additional confirmed P1/P2 was found. The unrelated intermittent
existing UI-test observation remains recorded, not silently relabeled a fix. Final automated
checks passed 760 tests with warnings/errors 0; production source did not change afterward.
Commit/push is authorized by the user's follow-up request after these checks.
